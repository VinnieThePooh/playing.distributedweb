using Confluent.Kafka;
using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Jaeger.Senders;
using Jaeger.Senders.Thrift;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using OpenTracing;
using OpenTracing.Util;
using Web.HostedServices;
using Web.HostedServices.Interfaces;
using Web.MessagingModels;
using Web.MessagingModels.Models;
using Web.MessagingModels.Options;
using Web.Services.Interfaces;
using Web.Services.Kafka.Consumers;
using Web.Services.Rest;

namespace Web.NodeThree
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
			services.Configure<RestTalkOptions>(Configuration.GetSection("RestTalkOptions"));

			services.AddSingleton<IRestSender<SampleMessage>, RestSender>();
			services.AddSingleton<ISampleMessageConsumer<Ignore>, SampleMessageConsumer>();
			services.AddSingleton<IKafkaConsumerService, KafkaConsumerService>();
			services.AddSingleton<IHostedService>(f => f.GetService<IKafkaConsumerService>());

			services.AddOpenTracing();
			services.AddSingleton<ITracer>(serviceProvider => {
				var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
				var config = Jaeger.Configuration.FromIConfiguration(loggerFactory, Configuration.GetSection("JaegerSettings"));
				var tracer = config.GetTracer();
				GlobalTracer.Register(tracer);
				return tracer;
			});

			services.AddControllers();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web.NodeThree", Version = "v1" });
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web.NodeThree v1"));
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
