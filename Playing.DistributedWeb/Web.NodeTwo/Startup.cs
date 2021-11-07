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
using OpenTracing;
using Microsoft.Extensions.Logging;
using Jaeger.Senders;
using Jaeger.Senders.Thrift;
using Jaeger.Samplers;
using Jaeger;
using OpenTracing.Util;

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
			services.AddOpenTracing();
			services.AddSingleton<ITracer>(serviceProvider => {				
				
				var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
				Jaeger.Configuration.SenderConfiguration.DefaultSenderResolver = new SenderResolver(loggerFactory)
				.RegisterSenderFactory<ThriftSenderFactory>();

				var config = Jaeger.Configuration.FromIConfiguration(loggerFactory, Configuration.GetSection("JaegerSettings"));
				var tracer = config.GetTracer();
				GlobalTracer.Register(tracer);
				return tracer;
			});
			services.AddSingleton<ISampleMessageProducer<Null>, SampleMessageProducer>();

			services.Configure<KafkaOptions>(Configuration.GetSection("KafkaOptions"));
			services.Configure<WebSocketServerOptions>(Configuration.GetSection("WebSocketServer"));
			services.AddControllers();	
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			Console.WriteLine($"Environment (from startup): {env.EnvironmentName}");

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
		}		

		
	}
}
