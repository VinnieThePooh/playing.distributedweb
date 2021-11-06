using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.MessagingModels.Options;

namespace Web.NodeTwo
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			Console.Title = "Web.NodeTwo";
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
			var postfix = envName == "Production" ? string.Empty : envName + ".";

			var host = CreateHostBuilder(args).Build();
			var conf = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile($"appsettings.{postfix}json", false)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();

			Console.WriteLine($"Environment: {envName}");

			var options = conf.GetSection("KafkaOptions").Get<KafkaOptions>();
			Console.WriteLine($"Kafka bootstrap server Url: {options.BootstrapServerUrl}");
			await EnsureKafkaTopicExists(host, options?.TopicName);
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
		
		//todo: implement it later or reconsider for others ways (built-in)
		private static async Task EnsureKafkaTopicExists(IHost host, string topicName)
		{
			if (string.IsNullOrEmpty(topicName))
				throw new ArgumentNullException(nameof(topicName));
		}
	}
}
