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
using OpenTracing;

namespace Web.HostedServices
{
	public class KafkaConsumerService : BackgroundService, IKafkaConsumerService
	{
		private CancellationToken consumingCancellationToken => SampleMessageConsumer.Token;
		private readonly ITracer _tracer;

		public KafkaConsumerService(ISampleMessageConsumer<Ignore> consumer, IRestSender<SampleMessage> restSender, ITracer tracer)
		{
			SampleMessageConsumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
			RestSender = restSender ?? throw new ArgumentNullException(nameof(restSender));
			_tracer = tracer;
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
					ISpan started = null;
					try
					{
						consumingCancellationToken.ThrowIfCancellationRequested();
						//blocking operation
						var consumeResult = SampleMessageConsumer.Consume(consumingCancellationToken);
						var message = consumeResult.Message.Value;
						message.NodeThree_Timestamp = DateTime.Now;

#if DEBUG
						var json = message.ToJson();
						var traceMessage = $"{++counter}. Kafka consumer received: {json}";					
						Console.WriteLine(traceMessage);
#endif
												
						var span = _tracer.BuildSpan("Adding to Batch").WithStartTimestamp(DateTime.Now);
						started = span.Start();
						// try to trace data sending
						await RestSender.AddToBatch(message);
						started.Finish(DateTime.Now);

					}
					catch (OperationCanceledException e)
					{
						started.Finish(DateTime.Now);
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
