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

namespace Web.HostedServices
{
	public class WebSocketClientService : BackgroundService, IWebSocketClientService
	{
		private static Random _random = new Random();
		private ClientWebSocket _clientWebSocket;
		private CancellationTokenSource _stoppingMessagingCts;
		private ManualResetEvent _manualReset = new ManualResetEvent(false);
		private readonly ISampleMessageRepository _messageRepository;
		private int _lastSessionId;
		private SendingStatistics _statistics;
		private Stopwatch _stopwatch;
		//todo: instead of this add State as enum with of states
		private bool _isWaitingForGracefulClose = false;

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


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();

			// only once: MessagingOptions.Duration is not changed in runtime
			_statistics = new SendingStatistics(MessagingOptions.Duration);
			_stopwatch = new Stopwatch();

			while (!stoppingToken.IsCancellationRequested)
			{
				CancellationToken opToken;
				try
				{
					_manualReset.WaitOne();
					_lastSessionId = await _messageRepository.GetCachedLastSessionId();

					//todo: use NLog or Serilog for logging
					Console.WriteLine($"[WebSocketClientService]: Service has begun new session: {_lastSessionId + 1}");

					_stopwatch.Start() ;
					opToken = _stoppingMessagingCts.Token;
					await EnsureSocketInitialized(opToken);				
					await StartMessagingSession(opToken, _lastSessionId + 1);

					if (MessagingOptions.PauseBettweenMessages > 0)
						await Task.Delay(MessagingOptions.PauseBettweenMessages);
				}
				catch (OperationCanceledException ce)
				{
					await _messageRepository.SetCachedLastSessionId(_lastSessionId + 1);
					// and only now we begin to listen carefully for a graceful close
					// do not begin new session until graceful close previous one
					await ListenForGracefulClose(_clientWebSocket);			
				}
				catch (WebSocketException wse)
				{
					//todo: handle this					
					//retry
					//do log here						
				}
				catch (Exception e)
				{
					//todo: 
					//do log here
				}
			}
		}

		private async Task StartMessagingSession(CancellationToken stopMessagingToken, int newSessionId)
		{			
			var workingTask = Task.Run(() => DoMessaging(stopMessagingToken, newSessionId), stopMessagingToken);

			var ms = MessagingOptions.Duration * 1000;

			var delay = Task.Delay(ms);

			//not sure ConfigureAwait(false) works with TaskScheduler.Default (highly likely NO)
			await delay.WithStopwatchRacing(TimeSpan.FromMilliseconds(ms)).ConfigureAwait(false);

#if DEBUG
			Console.WriteLine($"[WebSocketClientService]: Specified session interval expired after: {_stopwatch.ElapsedMilliseconds} ms");
#endif
			await StopMessaging();
			await workingTask;
		}


		public async ValueTask<OperResult> StartMessaging()
		{
			//todo: instead of this check state (will be added later) object:
			if (_stoppingMessagingCts != null)
				return new OperResult(OperResult.Messages.SendingData);

			if (_isWaitingForGracefulClose)
				return new OperResult(OperResult.Messages.WaitingForGracefulClose);
			
			_stoppingMessagingCts = new CancellationTokenSource();
			_manualReset.Set();
			return OperResult.Succeeded;
		}

		public async ValueTask<OperResult> StopMessaging()
		{
			//todo: instead of this check state (will be added later) object:
			if (_stoppingMessagingCts == null)
				return new OperResult(OperResult.Messages.AlreadyStopped);

			_manualReset.Reset();
			_stoppingMessagingCts.Cancel();		
			_stoppingMessagingCts.Dispose();
			_stoppingMessagingCts = null;
			return OperResult.Succeeded;
		}		

		private async Task DoMessaging(CancellationToken stopMessagingToken, int sessionId)
		{			
			int withinSessionMessageId = 1;

			// no need to depend on possibly complex state of the Network object (_clientWebSocket.State)
			// cause of we only send data
			// so, much better to depend on plain and simple CancellationToken (i.e. no network-bound local state)
			while (!stopMessagingToken.IsCancellationRequested)
			{				
				var message = new SampleMessage
				{
					NodeOne_Timestamp = DateTime.Now,
					SessionId = sessionId,
					WithinSessionMessageId = withinSessionMessageId++,
				};

				await SendMessage(message, stopMessagingToken);

				if (stopMessagingToken.IsCancellationRequested)
					break;
			}			

			//WARNING: actual session interval is much longer than specified (38.5 seconds vs 5s from config)
			// session completes here (this side only)
			// save statistics here
			_stopwatch.Stop();
			_statistics.ActualDuration = _stopwatch.ElapsedMilliseconds;
			_statistics.MessagesHandled = withinSessionMessageId;

			//todo: write log asyncronously?
			//todo: use NLog or Serilog for logging
			Console.WriteLine($"[WebSocketClientService]: Service ended session: {sessionId}");
			Console.WriteLine("Statistics:");
			Console.WriteLine($"{new string(' ', 3)}Data sending duration:");
			Console.WriteLine($"{new string(' ', 3)}1.Formal: {_statistics.FormalDuration}s");
			Console.WriteLine($"{new string(' ', 3)}2.Actual: {_statistics.ActualDuration} ms ({(double)_statistics.ActualDuration/1000:N1}s)");

			// initiate a graceful close operation
			await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);

			//todo: instead of set new State here
			_isWaitingForGracefulClose = true;

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

		private async Task ListenForGracefulClose(ClientWebSocket webSocket)
		{
			var buffer = new byte[4 * 1024];
			var arraySegment = new ArraySegment<byte>(buffer);
			
			var counter = 1;
			while (webSocket.State == WebSocketState.CloseSent)
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
						_isWaitingForGracefulClose = false;
						_stopwatch.Stop();
						_statistics.GracefulCloseInterval = _stopwatch.ElapsedMilliseconds;
						var closeInt = (double)_statistics.GracefulCloseInterval / 1000;
						var totalTime = (double)_statistics.TotalSessionTime / 1000;
						Console.WriteLine($"{new string(' ', 3)}3.Graceful close event after: {_statistics.GracefulCloseInterval} ms ({closeInt:N1}s, {closeInt/60:N1}m)");
						Console.WriteLine($"{new string(' ', 3)}4.Total session duration: {_statistics.TotalSessionTime} ms ({totalTime:N1}s, {totalTime / 60:N1}m)");
						break;
					}
				}
				catch (Exception)
				{
					throw;
				}			
				
			}			
		}

		private async Task SendMessage(SampleMessage message, CancellationToken stopMessagingToken)
		{
			if (stopMessagingToken.IsCancellationRequested)
				return;

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
