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

		public WebSocketClientService(IOptions<MessagingOptions> options, IOptions<WebSocketConnectionOptions> connectionOptions, ISampleMessageRepository messageRepository)
		{
			MessagingOptions = options.Value;
			ConnectionOptions = connectionOptions.Value;
			_messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
		}

		public MessagingOptions MessagingOptions { get; }

		public WebSocketConnectionOptions ConnectionOptions { get; }

		public bool IsMessaging => _stoppingMessagingCts != null;


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();

			// only once, it is not changed in runtime
			_statistics.FormalDuration = MessagingOptions.Duration;
			_stopwatch = new Stopwatch();

			while (!stoppingToken.IsCancellationRequested)
			{
				CancellationToken opToken;
				try
				{
					_manualReset.WaitOne();

					//todo: use NLog or Serilog for logging
					Console.WriteLine($"WebSocketClientService has begun new session: {_lastSessionId + 1}");

					_stopwatch.Start() ;
					opToken = _stoppingMessagingCts.Token;
					await EnsureSocketInitialized(opToken);
					_lastSessionId = await _messageRepository.GetCachedLastSessionId();
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
			await Task.Delay(MessagingOptions.Duration * 1000);
			await StopMessaging();
			await workingTask;
		}


		public async Task<bool> StartMessaging()
		{
			if (_stoppingMessagingCts != null)
				return false;
			
			_stoppingMessagingCts = new CancellationTokenSource();
			_manualReset.Set();
			return true;
		}

		public async Task<bool> StopMessaging()
		{
			if (_stoppingMessagingCts == null)
				return false;

			_manualReset.Reset();
			_stoppingMessagingCts.Cancel();			
			_stoppingMessagingCts.Dispose();
			_stoppingMessagingCts = null;			
			return true;
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

			// session completes here (this side only)
			// save statistics here
			_stopwatch.Stop();
			_statistics.ActualDuration = _stopwatch.ElapsedMilliseconds;
			_statistics.MessagesHandled = withinSessionMessageId;
			_stopwatch.Reset();

			//todo: use NLog or Serilog for logging
			Console.WriteLine($"WebSocketClientService ended session: {sessionId}");
			Console.WriteLine("Statistics:");
			Console.WriteLine($"{new string(' ', 3)}Duration:");
			Console.WriteLine($"{new string(' ', 3)}1.Formal: {_statistics.FormalDuration}s");
			Console.WriteLine($"{new string(' ', 3)}2.Actual: {_statistics.ActualDuration}ms");

			// initiate a graceful close operation
			await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);			
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
