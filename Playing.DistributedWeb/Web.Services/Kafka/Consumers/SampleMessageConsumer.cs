using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using Web.MessagingModels;
using Web.MessagingModels.Interfaces;
using Web.MessagingModels.Models;
using Web.MessagingModels.Options;

namespace Kafka.Consumers
{
	public class SampleMessageConsumer : ISampleMessageConsumer<KafkaMessageId>, IHasKafkaOptions, IDisposable
	{
		private IConsumer<KafkaMessageId, SampleMessage> _consumer;

		public SampleMessageConsumer(IOptions<KafkaOptions> kafkaOptions)
		{
			KafkaOptions = kafkaOptions.Value;
			var config = new ConsumerConfig
			{
				BootstrapServers = KafkaOptions.BootstrapServerUrl,
				ClientId = KafkaOptions.ClientId,			
			};

			_consumer = new ConsumerBuilder<KafkaMessageId, SampleMessage>(config).Build();			
		}

		public KafkaOptions KafkaOptions { get; }	

		public ConsumeResult<KafkaMessageId, SampleMessage> Consume(CancellationToken token)
		{
			return _consumer.Consume(token);
		}

		public IAsyncEnumerable<ConsumeResult<KafkaMessageId, SampleMessage>> ConsumeRange(CancellationToken token)
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
