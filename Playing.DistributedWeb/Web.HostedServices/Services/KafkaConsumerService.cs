using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Web.HostedServices.Interfaces;
using Web.Services.Kafka.Consumers;
using Web.MessagingModels.Extensions;
using Confluent.Kafka;
using Web.MessagingModels;
using Web.Services.Interfaces;
using OpenTracing;
using Web.MessagingModels.Constants;

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

						var now = DateTime.Now;

						var builder = _tracer
							.BuildSpan(OperationNames.KAFKA_MESSAGE_CONSUMED)
							.WithTag(JaegerTagNames.NodeThree, JaegerTagValues.NodeThree.KAFKA_MESSAGE_CONSUMED);
						started = builder.Start();

						var message = consumeResult.Message.Value;
						message.NodeThree_Timestamp = now;

#if DEBUG
						var json = message.ToJson();
						var traceMessage = $"{++counter}. Kafka consumer received: {json}";					
						Console.WriteLine(traceMessage);
#endif
						await RestSender.AddToBatch(message);
						started.Finish(DateTime.Now);

					}
					catch (OperationCanceledException e)
					{
						//todo: serilog here
						var logMessage = $"{OperationNames.WORK_CANCELLED}: {nameof(KafkaConsumerService)}: working was cancelled";
						started.Log(DateTimeOffset.Now, logMessage);
						started.Finish(DateTime.Now);
					}
					catch(Exception e)
					{
						//todo: serilog here
						var logMessage = $"{OperationNames.WORK_FAILED}: {nameof(KafkaConsumerService)}: Error: {e}";
						started.Log(DateTimeOffset.Now, logMessage);
						started.Finish(DateTimeOffset.Now);
					}
				}
			}
		}
	}	
}
