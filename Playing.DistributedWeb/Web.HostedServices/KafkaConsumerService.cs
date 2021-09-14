using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Web.HostedServices
{
	public class KafkaConsumerService : BackgroundService
	{
		public KafkaConsumerService()
		{

		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();

			while(!stoppingToken.IsCancellationRequested)
			{

			}
		}
	}
}
