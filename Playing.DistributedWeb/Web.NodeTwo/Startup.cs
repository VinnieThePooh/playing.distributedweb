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

			services.AddSingleton<ISampleMessageProducer<KafkaMessageId>, SampleMessageProducer>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			var webSocketOptions = Configuration.GetSection("WebSocketServer").Get<WebSocketServerOptions>();

			var kafkaProducer = app.ApplicationServices.GetService<ISampleMessageProducer<KafkaMessageId>>();

			


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
			app.Use(async (context, next) => {

				if (context.Request.Path != socketPath)
				{
					await next();
					return;
				}

				if (!context.WebSockets.IsWebSocketRequest)
					context.Response.StatusCode = StatusCodes.Status400BadRequest;
							
				using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync())
				{
					await HandleWebSocketDataDemo(context, webSocket, kafkaProducer);
				}				
			});
		}
		
		private async Task HandleWebSocketDataDemo(HttpContext context, WebSocket webSocket, ISampleMessageProducer<KafkaMessageId> producer)
		{
			var buffer = new byte[1024 * 4];
			ArraySegment<byte> bytesSegment = new ArraySegment<byte>(buffer);
			WebSocketReceiveResult result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);
			
			var bytes = new List<byte>();

			//while (!webSocket.CloseStatus.HasValue)
			while (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseSent)
			{
				bytes.AddRange(bytesSegment.Slice(0, result.Count));
				//Console.WriteLine($"Received data, bytes count: {result.Count}");
				//Console.WriteLine($"End of message: {result.EndOfMessage}");
				//Console.WriteLine($"MessageType: {result.MessageType}");				

				Console.WriteLine($"WebSocket.State: {webSocket.State}");
				Console.WriteLine($"WebSocketReceiveResult.CloseStatus: {(result.CloseStatus.HasValue ? result.CloseStatus.Value: "NULL")}");

				SampleMessage message;

				if (result.EndOfMessage)
				{
#if DEBUG
					var bytesArray = bytes.ToArray();
					var str = Encoding.UTF8.GetString(bytesArray);
					Debug.WriteLine(str);
#endif

					message = (SampleMessage)await JsonSerializer.DeserializeAsync(new MemoryStream(bytesArray), typeof(SampleMessage), null, CancellationToken.None);
					Debug.WriteLine(message.ToString());

					var deliveryResult = await producer.ProduceAsync(message, CancellationToken.None);

					bytes.Clear();
				}

				result = await webSocket.ReceiveAsync(bytesSegment, CancellationToken.None);
			}
			//await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
		}
	}
}
