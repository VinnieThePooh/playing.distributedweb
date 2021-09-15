using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Web.MessagingModels.Options;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Web.MessagingModels;
using System.IO;
using System.Diagnostics;
using System.Text;
using Web.MessagingModels.Models;
using Web.Services.Kafka.Producers;
using Confluent.Kafka;
using Web.MessagingModels.Extensions;
using System.Runtime.CompilerServices;

namespace Web.NodeTwo
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<KafkaOptions>(Configuration.GetSection("KafkaOptions"));
			services.Configure<WebSocketServerOptions>(Configuration.GetSection("WebSocketServer"));
			services.AddControllers();

			services.AddSingleton<ISampleMessageProducer<Null>, SampleMessageProducer>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			var webSocketOptions = Configuration.GetSection("WebSocketServer").Get<WebSocketServerOptions>();
			var kafkaProducer = app.ApplicationServices.GetService<ISampleMessageProducer<Null>>();


			//todo: add other options from config
			var socketOptions = new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(webSocketOptions.KeepAliveInterval),			
			};

			app.UseWebSockets();

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();			
			}

			app.UseHttpsRedirection();
			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			// /ws - default path
			var socketPath = !string.IsNullOrEmpty(webSocketOptions?.SocketPath) ? webSocketOptions.SocketPath : "/ws";
			
			//todo: move to a controller with a static route
			app.Use(async (context, next) => 
			{
				if (context.Request.Path != socketPath)
				{
					await next();
					return;
				}

				if (!context.WebSockets.IsWebSocketRequest)
					context.Response.StatusCode = StatusCodes.Status400BadRequest;

				context.Response.StatusCode = StatusCodes.Status101SwitchingProtocols;
				context.Response.Headers.Add("Upgrade", "websocket");

				var webSocket = await context.WebSockets.AcceptWebSocketAsync();
				await HandleWebSocketDataDemo(context, webSocket, kafkaProducer);	
			});
		}
		
		private async Task HandleWebSocketDataDemo(HttpContext context, WebSocket webSocket, ISampleMessageProducer<Null> producer)
		{
			var buffer = new byte[1024 * 4];
			ArraySegment<byte> bytesSegment = new ArraySegment<byte>(buffer);
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);
			
			var bytes = new List<byte>();

			var counter = 1;

			while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
			{
				bytes.AddRange(bytesSegment.Slice(0, result.Count));

				SampleMessage message;

				if (CheckForCloseIntention(ref result))
				{
					// confirm close handshake
					await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
					break;
				}

				if (result.EndOfMessage)
				{
					message = (SampleMessage)await JsonSerializer.DeserializeAsync(new MemoryStream(bytes.ToArray()), typeof(SampleMessage), null, CancellationToken.None);
					message.NodeTwo_Timestamp = DateTime.Now;

					var deliveryResult = await producer.ProduceAsync(message, CancellationToken.None);
					bytes.Clear();
#if DEBUG
					Console.WriteLine($" Web.NodeTwo: {counter++}. WebSocket server received and kafka producer retransmitted: {message.ToJson()}");
#endif
				}

				result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);

				if (CheckForCloseIntention(ref result))
				{					
					await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
					break;
				}
			}			
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool CheckForCloseIntention(ref WebSocketReceiveResult receiveResult)
		{
			return receiveResult.CloseStatus != null || receiveResult.MessageType == WebSocketMessageType.Close;
		}
	}
}
