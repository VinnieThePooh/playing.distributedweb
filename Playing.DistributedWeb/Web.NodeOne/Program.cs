using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Web.DataAccess;

namespace Web.NodeOne
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var conf = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json")
				.Build();

			var connString = conf.GetConnectionString("MariaDb");

			await SimpleMariaDbInitializer.Instance.InitializeDb(connString);

			var host = CreateHostBuilder(args).Build();
			host.Run();				
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});				
	}
}
