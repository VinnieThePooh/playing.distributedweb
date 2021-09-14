using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using Web.MessagingModels;
using Web.MessagingModels.Interfaces;
using Web.MessagingModels.Options;
using Web.Services.Kafka.Serialization;

namespace Web.Services.Kafka.Consumers
{
	public class SampleMessageConsumer : ISampleMessageConsumer<Ignore>, IHasKafkaOptions, IDisposable
	{
		private IConsumer<Ignore, SampleMessage> _consumer;
		private CancellationTokenSource _cts = new CancellationTokenSource();

		public SampleMessageConsumer(IOptions<KafkaOptions> kafkaOptions)
		{
			KafkaOptions = kafkaOptions.Value;
			var config = new ConsumerConfig
			{
				BootstrapServers = KafkaOptions.BootstrapServerUrl,
				ClientId = KafkaOptions.ClientId,		
				GroupId = "importance: high",			
			};
			_consumer = new ConsumerBuilder<Ignore, SampleMessage>(config)
			.SetValueDeserializer(new CustomDeserializer<SampleMessage>())
			.Build();			
		}

		public KafkaOptions KafkaOptions { get; }

		public CancellationToken Token => _cts?.Token ?? CancellationToken.None;

		public ConsumeResult<Ignore, SampleMessage> Consume(CancellationToken token)
		{
			return _consumer.Consume(token);
		}

		public IAsyncEnumerable<ConsumeResult<Ignore, SampleMessage>> ConsumeRange(CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			if (_consumer != null)
			{
				_consumer.Dispose();
				_consumer = null;
			}
		}
	}
}
