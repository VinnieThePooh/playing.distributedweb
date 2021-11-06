using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Web.DataAccess;
using Web.DataAccess.Interfaces;
using Web.DataAccess.Repositories;
using Console = System.Console;

namespace Web.NodeOne
{
	public class Program
	{
		private static object lockObject = new object();

		public static async Task Main(string[] args)
		{
			Console.Title = "Web.NodeOne";
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			
			var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
			var postfix = envName == "Production" ? string.Empty : envName + ".";

			var conf = new ConfigurationBuilder()
				.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile($"appsettings.{postfix}json", false)
				.AddEnvironmentVariables()
				.AddCommandLine(args)
				.Build();

			var connString = conf.GetConnectionString("MariaDb");
			Console.WriteLine($"Connection String: {connString}");

			Console.WriteLine($"Environment: {envName}");

			await SimpleMariaDbInitializer.Instance.InitializeDb(connString);

			var host = CreateHostBuilder(args).Build();

			var repo = (ISampleMessageRepository) host.Services.GetService(typeof(ISampleMessageRepository));
			await repo.SetCachedLastSessionId(await repo.GetLastSessionId());
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
