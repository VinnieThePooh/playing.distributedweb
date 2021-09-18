using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Web.MessagingModels;
using Web.MessagingModels.Options;
using System.Net.WebSockets;
using Web.HostedServices.Interfaces;
using Web.DataAccess.Interfaces;
using System.Text.Json;
using System.IO;
using Web.MessagingModels.Models;
using System.Diagnostics;
using Web.MessagingModels.Extensions.Tasks;
using Web.HostedServices.Models;
using System.Runtime.CompilerServices;

namespace Web.HostedServices
{
	public class WebSocketClientService : BackgroundService, IWebSocketClientService
	{		
		private ClientWebSocket _clientWebSocket;
		private CancellationTokenSource _stoppingMessagingCts;
		private ManualResetEvent _manualReset = new ManualResetEvent(false);
		private readonly ISampleMessageRepository _messageRepository;
		private int _lastSessionId;
		private SendingStatistics _statistics;
		private Stopwatch _stopwatch = new Stopwatch();
		
		private object lockObject = new object();
		ServiceState _serviceState = default;

		public WebSocketClientService(IOptions<MessagingOptions> options, IOptions<WebSocketConnectionOptions> connectionOptions, ISampleMessageRepository messageRepository)
		{
			MessagingOptions = options.Value;
			ConnectionOptions = connectionOptions.Value;
			_messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
		}

		public MessagingOptions MessagingOptions { get; }

		public WebSocketConnectionOptions ConnectionOptions { get; }

		//todo: instead of this add State as enum with of states
		public bool IsMessaging => _stoppingMessagingCts != null;

		public ServiceState ServiceState
		{ 
			get 
			{ 
			  lock(lockObject)
				return _serviceState;
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();
			while (!stoppingToken.IsCancellationRequested)
			{
				CancellationToken opToken;
				try
				{
					_manualReset.WaitOne();
					_lastSessionId = await _messageRepository.GetCachedLastSessionId();
					
					//todo: Serilog here
					Console.WriteLine($"[WebSocketClientService]: Service has begun new session: {_lastSessionId + 1}");

					_stopwatch.Start();
					opToken = _stoppingMessagingCts.Token;
					await EnsureSocketInitialized(opToken);
					await StartMessagingSession(opToken, _lastSessionId + 1);

					if (MessagingOptions.PauseBettweenMessages > 0)
						await Task.Delay(MessagingOptions.PauseBettweenMessages);
				}
				catch (OperationCanceledException ce)
				{
					//todo: Serilog here
				}
				catch (WebSocketException wse)
				{
					//todo: Serilog here											
				}
				catch (Exception e)
				{
					//todo: Serilog here					
				}
			}
		}

		private async Task StartMessagingSession(CancellationToken stopMessagingToken, int newSessionId)
		{			
			var workingTask = Task.Run(() => DoMessaging(stopMessagingToken, newSessionId), stopMessagingToken);

			//not sure ConfigureAwait(false) works with TaskScheduler.Default (highly likely NO)
			await Task.Delay(MessagingOptions.Duration * 1000).ConfigureAwait(false);

#if DEBUG
			Console.WriteLine($"[WebSocketClientService]: Specified session interval expired after: {_stopwatch.ElapsedMilliseconds} ms");
#endif

			await StopMessaging();
			await _messageRepository.SetCachedLastSessionId(_lastSessionId + 1);
			await ListenForGracefulClose();
		}


		public async ValueTask<OperResult> StartMessaging()
		{
			lock(lockObject)
			{
				if (_stoppingMessagingCts != null)
				{
					if (_stoppingMessagingCts.IsCancellationRequested)
						return new OperResult(OperResult.Messages.WaitingForGracefulClose);

					return new OperResult(OperResult.Messages.SendingData);
				}

				_stopwatch.Reset();
				_statistics = new SendingStatistics(MessagingOptions.Duration);
				_stoppingMessagingCts = new CancellationTokenSource();
				_manualReset.Set();
				_serviceState = ServiceState.SendingData;
				return OperResult.Succeeded;
			}						
		}

		public async ValueTask<OperResult> StopMessaging()
		{
			var res = StopMessagingInternal();
			if (res.IsSucceeded)
			{
				await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
				lock(lockObject)
					_serviceState = ServiceState.WaitingForGracefulClose;
			}
			return res;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private OperResult StopMessagingInternal()
		{
			lock (lockObject)
			{
				if (_stoppingMessagingCts == null)
					return new OperResult(OperResult.Messages.AlreadyStopped);

				if (_stoppingMessagingCts.IsCancellationRequested)
					return new OperResult(OperResult.Messages.WaitingForGracefulClose);

				_manualReset.Reset();
				_stoppingMessagingCts.Cancel();
				_stoppingMessagingCts.Dispose();				
			}

			return OperResult.Succeeded;
		}


		private async Task DoMessaging(CancellationToken stopMessagingToken, int sessionId)
		{			
			int withinSessionMessageId = 1;

			//only ServiceState.SendingData phase can be stopped
			while (ServiceState == ServiceState.SendingData)
			{
				var message = new SampleMessage
				{
					NodeOne_Timestamp = DateTime.Now,
					SessionId = sessionId,
					WithinSessionMessageId = withinSessionMessageId++,
				};

				await SendMessage(message);
				_statistics.MessagesHandled++;

				if (stopMessagingToken.IsCancellationRequested)
					break;
			}
			
			_stopwatch.Stop();
			_statistics.ActualDuration = _stopwatch.ElapsedMilliseconds;
			
			//todo: Serilog here
			Console.WriteLine($"[WebSocketClientService]: Service ended session: {sessionId}");
			Console.WriteLine("Statistics:");
			Console.WriteLine($"{new string(' ', 3)}Data sending duration:");
			Console.WriteLine($"{new string(' ', 3)}1.Formal: {_statistics.FormalDuration}s");

			var actDur = (double)_statistics.ActualDuration / 1000;

			Console.WriteLine($"{new string(' ', 3)}2.Actual: {_statistics.ActualDuration} ms ({actDur:N1}s, {actDur/60:N1}m)");			

			//continue to measure graceful close interval
			_stopwatch.Restart();
			stopMessagingToken.ThrowIfCancellationRequested();
		}

		//todo: add network problems handling (retry)
		//dispose and recreate socket or keep connection opened?
		private async Task EnsureSocketInitialized(CancellationToken stopMessagingToken)
		{
			if (_clientWebSocket == null)
			{
				_clientWebSocket = new ClientWebSocket();				
				await _clientWebSocket.ConnectAsync(new Uri(ConnectionOptions.SocketUrl), stopMessagingToken);
			}
		}

		private async Task ListenForGracefulClose()
		{
			var buffer = new byte[4 * 1024];
			var arraySegment = new ArraySegment<byte>(buffer);
			var webSocket = _clientWebSocket;
			
			var counter = 1;

			//only once access to the property
			var state = ServiceState;
			while (state == ServiceState.SendingData ||
				   state == ServiceState.WaitingForGracefulClose)
			{
				Debug.WriteLine($"{counter++}. Listening for WebSocketMessageType.Close command");
				WebSocketReceiveResult result;

				try
				{
					result = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);
					
					if (result.MessageType == WebSocketMessageType.Close)
					{
						//graceful close completes here
						//todo: later just set new State here
						lock(lockObject)
						{
							_serviceState = ServiceState.Stopped;
							_stoppingMessagingCts = null;
							_clientWebSocket.Dispose();
							_clientWebSocket = null;
						}
						
						_stopwatch.Stop();
						_statistics.GracefulCloseInterval = _stopwatch.ElapsedMilliseconds;
						var closeInt = (double)_statistics.GracefulCloseInterval / 1000;
						var totalTime = (double)_statistics.TotalSessionTime / 1000;
						Console.WriteLine($"{new string(' ', 3)}3.Graceful close event after: {_statistics.GracefulCloseInterval} ms ({closeInt:N1}s, {closeInt/60:N1}m)");
						Console.WriteLine($"{new string(' ', 3)}4.Total session duration: {_statistics.TotalSessionTime} ms ({totalTime:N1}s, {totalTime / 60:N1}m)");
						Console.WriteLine($"{new string(' ', 3)}Messages handling:");
						Console.WriteLine($"{new string(' ', 3)}5.Total messages sent: {_statistics.MessagesHandled}");
						break;
					}
					state = ServiceState;
				}
				catch (Exception)
				{
					lock (lockObject)
					{
						_serviceState = ServiceState.Stopped;
					}
					//todo: Serilog here
				}				
			}			
		}

		private async Task SendMessage(SampleMessage message)
		{
			ArraySegment<byte> bytes;
			using (var stream = new MemoryStream())
			{
				//JsonSerializer a little bit faster than Newtonsoft.Json
				await JsonSerializer.SerializeAsync(stream, message, null, CancellationToken.None);
				bytes = new ArraySegment<byte>(stream.ToArray());
			}
			await _clientWebSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}
