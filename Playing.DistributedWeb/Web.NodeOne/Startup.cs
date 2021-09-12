using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
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

			services.AddSingleton<IWebSocketClientService, WebSocketClientService>();

			//services.AddSingleton<IHostedService>(p => p.GetService<WebSocketClientService>());

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
