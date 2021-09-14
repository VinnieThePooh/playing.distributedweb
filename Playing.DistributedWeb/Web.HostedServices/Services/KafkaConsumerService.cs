using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Web.HostedServices.Interfaces;
using Web.MessagingModels.Models;
using Web.Services.Kafka.Consumers;
using Web.MessagingModels.Extensions;
using System.Diagnostics;
using Confluent.Kafka;

namespace Web.HostedServices
{
	public class KafkaConsumerService : BackgroundService, IKafkaConsumerService
	{
		private CancellationToken consumingCancellationToken => SampleMessageConsumer.Token;

		public KafkaConsumerService(ISampleMessageConsumer<Ignore> consumer)
		{
			SampleMessageConsumer = consumer;
		}

		public ISampleMessageConsumer<Ignore> SampleMessageConsumer { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();

			while(!stoppingToken.IsCancellationRequested)
			{
				while(true)
				{
					try
					{
						consumingCancellationToken.ThrowIfCancellationRequested();
						var consumeResult = SampleMessageConsumer.Consume(consumingCancellationToken);
						var json = consumeResult.Message.Value.ToJson();
						Debug.WriteLine($"Got: {json}");
					}
					catch (OperationCanceledException e)
					{						
						//do nothing
					}
					catch(Exception e)
					{
						//do log here
					}
				}
			}
		}
	}	
}
