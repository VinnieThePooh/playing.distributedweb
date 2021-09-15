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
using Web.MessagingModels;
using Microsoft.Extensions.Options;
using Web.MessagingModels.Options;
using Web.Services.Interfaces;

namespace Web.HostedServices
{
	public class KafkaConsumerService : BackgroundService, IKafkaConsumerService
	{
		private CancellationToken consumingCancellationToken => SampleMessageConsumer.Token;

		public KafkaConsumerService(ISampleMessageConsumer<Ignore> consumer, IRestSender<SampleMessage> restSender)
		{
			SampleMessageConsumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
			RestSender = restSender ?? throw new ArgumentNullException(nameof(restSender));
		}

		public ISampleMessageConsumer<Ignore> SampleMessageConsumer { get; }
		public IRestSender<SampleMessage> RestSender { get; }

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();

			while(!stoppingToken.IsCancellationRequested)
			{
				int counter = 0;
				while(true)
				{
					try
					{
						consumingCancellationToken.ThrowIfCancellationRequested();
						var consumeResult = SampleMessageConsumer.Consume(consumingCancellationToken);
						var message = consumeResult.Message.Value;

#if DEBUG
						var json = message.ToJson();
						var traceMessage = $"{++counter}. Kafka consumer received: {json}";					
						Console.WriteLine(traceMessage);
#endif
						await RestSender.AddToBatch(message);

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
