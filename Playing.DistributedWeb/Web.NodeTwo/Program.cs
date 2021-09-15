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

			var host = CreateHostBuilder(args).Build();

			var conf = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.Build();

			var options = conf.GetSection("KafkaOptions")?.Get<KafkaOptions>();
			await EnsureKafkaTopicExists(host, options?.TopicName);

			host.Run();
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
