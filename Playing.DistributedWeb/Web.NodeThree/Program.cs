using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;

namespace Web.NodeThree
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Console.Title = "Web.NodeThree";
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			var host = CreateHostBuilder(args).Build();
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
