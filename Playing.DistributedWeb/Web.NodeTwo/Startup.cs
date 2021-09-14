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
			
			while (!webSocket.CloseStatus.HasValue)
			{
				TraceWebSocketStatus(webSocket);

				bytes.AddRange(bytesSegment.Slice(0, result.Count));				

				SampleMessage message;

				if (result.EndOfMessage)
				{
					message = (SampleMessage)await JsonSerializer.DeserializeAsync(new MemoryStream(bytes.ToArray()), typeof(SampleMessage), null, CancellationToken.None);
					message.NodeTwo_Timestamp = DateTime.Now;

#if DEBUG
					Debug.WriteLine($"Web.NodeTwo WebSocket received: {message.ToJson()}"); 
#endif

					var deliveryResult = await producer.ProduceAsync(message, CancellationToken.None);					
					//Debug.WriteLine(deliveryResult.Status);

					bytes.Clear();
				}
				
				if (webSocket.State == WebSocketState.CloseReceived)
				{
					Debugger.Break();

					await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
					break;
				}
				
				result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);
			}
			//await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
		}

		private void TraceWebSocketStatus(WebSocket webSocket)
		{
			Console.WriteLine($"ServerSocket close status: {(webSocket.CloseStatus.HasValue ? webSocket.CloseStatus.Value: "NULL")}");
			Console.WriteLine($"ServerSocket state: {webSocket.State}");
		}
	}
}
