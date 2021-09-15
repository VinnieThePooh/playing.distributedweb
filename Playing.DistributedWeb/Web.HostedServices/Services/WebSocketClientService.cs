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
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_manualReset.WaitOne();
					await EnsureSocketInitialized(_stoppingMessagingCts.Token);
					_lastSessionId = await _messageRepository.GetCachedLastSessionId();
					await StartMessagingSession(_stoppingMessagingCts.Token, _lastSessionId + 1);

					if (MessagingOptions.PauseBettweenMessages > 0)
						await Task.Delay(MessagingOptions.PauseBettweenMessages);
				}
				catch (OperationCanceledException ce)	
				{
					await _messageRepository.SetCachedLastSessionId(_lastSessionId + 1);
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
			
			var listenTask = ListenForGracefulClose(stopMessagingToken, _clientWebSocket);

			while (_clientWebSocket.State == WebSocketState.Open)
			{
				if (stopMessagingToken.IsCancellationRequested)
					break;

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
			// initiate a graceful close operation
			await _clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
			// and only now we begin to listen carefully for a graceful close
			// do not begin new session untill close previous one
			await ListenForGracefulClose(stopMessagingToken, _clientWebSocket);
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

		private async Task ListenForGracefulClose(CancellationToken stopMessagingToken, ClientWebSocket webSocket)
		{
			var buffer = new byte[4 * 1024];
			var arraySegment = new ArraySegment<byte>(buffer);
			
			var counter = 1;
			while (webSocket.State == WebSocketState.Open)
			{
				Debug.WriteLine($"{counter++}. Listening for WebSocketMessageType.Close command");
				WebSocketReceiveResult result;

				try
				{
					result = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);		
					// AAAARGGHH!! This never happens!
					// so we can't rely on
					if (result.MessageType == WebSocketMessageType.Close)
					{						
						//graceful close completes here
						//todo: log here result of the conversation:
						//	1.Elapsed time
						//	2.Number of messages sent						
						await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
						break;
					}
				}
				catch (Exception)
				{
					throw;
				}			
				
			}
			stopMessagingToken.ThrowIfCancellationRequested();
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

		private async Task SendInitClosingMessage()
		{
			ArraySegment<byte> bytes;
			using (var stream = new MemoryStream())
			{
				//JsonSerializer a little bit faster than Newtonsoft.Json
				await JsonSerializer.SerializeAsync(stream, new CommandMessage { CommandCode = CommandCode.CloseChannel }  , null, CancellationToken.None);
				bytes = new ArraySegment<byte>(stream.ToArray());
			}
			await _clientWebSocket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}
