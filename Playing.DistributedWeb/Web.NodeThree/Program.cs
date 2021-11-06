using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.Configuration;
using Web.MessagingModels.Options;

namespace Web.NodeThree
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Console.Title = "Web.NodeThree";
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			var conf = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
				.AddEnvironmentVariables()
				.Build();

			var host = CreateHostBuilder(args).Build();

			var options = conf.GetSection("KafkaOptions").Get<KafkaOptions>();
			Console.WriteLine($"Kafka bootstrap server Url: {options.BootstrapServerUrl}");

			host.Run();
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			//todo: log here
			Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});


	}
}
