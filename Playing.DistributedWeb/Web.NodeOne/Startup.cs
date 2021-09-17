using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTracing;
using OpenTracing.Util;
using System.Reflection;
using Web.DataAccess.Interfaces;
using Web.DataAccess.Repositories;
using Web.HostedServices;
using Web.HostedServices.Interfaces;
using Web.MessagingModels.Options;


namespace Web.NodeOne
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
			services.Configure<MessagingOptions>(Configuration.GetSection("Messaging"));
			services.Configure<WebSocketConnectionOptions>(Configuration.GetSection("Messaging:Nodes:WebSocket"));

			var conString = Configuration.GetConnectionString("MariaDb");

			services.AddOpenTracing();
			services.AddSingleton<ITracer>(serviceProvider => {
				var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
				var config = Jaeger.Configuration.FromIConfiguration(loggerFactory, Configuration.GetSection("JaegerSettings"));
				var tracer = config.GetTracer();
				GlobalTracer.Register(tracer);
				return tracer;
			});

			//todo: probably just non-IHostedService singleton works as well (or not?)
			services.AddSingleton<IWebSocketClientService, WebSocketClientService>();		
			services.AddSingleton<IHostedService>(p => p.GetService<IWebSocketClientService>());			
			services.AddSingleton<ISampleMessageRepository, MariaDbSampleMessageRepository>(sp => new MariaDbSampleMessageRepository(conString));
						
			services.AddControllers();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web.NodeOne", Version = "v1" });
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web.NodeOne v1"));
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
