using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenTracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Web.MessagingModels;
using Web.MessagingModels.Extensions;
using Web.MessagingModels.Models;
using Web.MessagingModels.Options;
using Web.Services.Kafka.Producers;

namespace Web.NodeTwo.Controllers
{	
	[ApiController]
	public class WebSocketsController : ControllerBase
	{
		ITracer _tracer;
		private readonly ISampleMessageProducer<Null> _producer;
		private KafkaOptions KafkaOptions { get; }
		private WebSocketServerOptions SocketOptions { get; }

		public WebSocketsController(ITracer tracer, ISampleMessageProducer<Null> producer, IOptions<KafkaOptions> kafkaOptions, IOptions<WebSocketServerOptions> socketServerOptions)
		{
			_tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));
			_producer = producer;
			KafkaOptions = kafkaOptions?.Value ?? throw new ArgumentNullException(nameof(kafkaOptions));
			SocketOptions = socketServerOptions?.Value ?? throw new ArgumentNullException(nameof(socketServerOptions));
		}

		[HttpGet("/ws")]
		public async Task Get()
		{
			if (!HttpContext.WebSockets.IsWebSocketRequest)
			{
				HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
				return;
			}

			HttpContext.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
			HttpContext.Response.Headers.Add("Upgrade", "websocket");

			var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
			await HandleWebSocketDataDemo(HttpContext, webSocket, _producer);
		}

		private async Task HandleWebSocketDataDemo(HttpContext context, WebSocket webSocket, ISampleMessageProducer<Null> producer)
		{			
			var buffer = new byte[1024 * 4];
			ArraySegment<byte> bytesSegment = new ArraySegment<byte>(buffer);
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);
			ReceivingStatistics statistics = new();

			var bytes = new List<byte>();

			var counter = 1;
			int? sessionId = null;

			Stopwatch sw = Stopwatch.StartNew();
			var span = _tracer.BuildSpan("web-socket-session-begin").WithStartTimestamp(DateTime.Now);
			var started = span.Start();
			while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
			{
				bytes.AddRange(bytesSegment.Slice(0, result.Count));

				SampleMessage message;

				if (CheckForCloseIntention(ref result))
				{
					sw.Stop();
					statistics.ActualDuration = sw.ElapsedMilliseconds;

					// confirm close handshake
					await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
					break;
				}

				if (result.EndOfMessage)
				{
					message = (SampleMessage)await JsonSerializer.DeserializeAsync(new MemoryStream(bytes.ToArray()), typeof(SampleMessage), null, CancellationToken.None);
					message.NodeTwo_Timestamp = DateTime.Now;
					if (sessionId is null)
						sessionId = message.SessionId;
					statistics.MessagesReceived++;

					var deliveryResult = await producer.ProduceAsync(message, CancellationToken.None);
					bytes.Clear();
#if DEBUG
					Console.WriteLine($" Web.NodeTwo: {counter++}. WebSocket server received and kafka producer retransmitted: {message.ToJson()}");
#endif
				}

				result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);

				if (CheckForCloseIntention(ref result))
				{
					sw.Stop();
					statistics.ActualDuration = sw.ElapsedMilliseconds;
					started.Finish(DateTime.Now);
					await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
					break;
				}
			}

			//session ends here
			//todo: Serilog here
			Console.WriteLine($"Server WebSocket ended session: {sessionId}");
			Console.WriteLine("Statistics:");
			Console.WriteLine($"{new string(' ', 3)}1.Duration: {statistics.ActualDuration}ms");
			Console.WriteLine($"{new string(' ', 3)}2.Messages received: {statistics.MessagesReceived}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool CheckForCloseIntention(ref WebSocketReceiveResult receiveResult)
		{
			return receiveResult.CloseStatus != null || receiveResult.MessageType == WebSocketMessageType.Close;
		}
	}
}
