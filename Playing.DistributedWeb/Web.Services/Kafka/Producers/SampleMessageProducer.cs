using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Web.MessagingModels;
using Web.MessagingModels.Interfaces;
using Web.MessagingModels.Models;
using Web.MessagingModels.Options;

namespace Web.Services.Kafka.Producers
{
	public class SampleMessageProducer: ISampleMessageProducer<KafkaMessageId>, IHasKafkaOptions, IDisposable
	{
		private IProducer<KafkaMessageId, SampleMessage> _producer;

		public SampleMessageProducer(IOptions<KafkaOptions> options)
		{
			if (options is null)			
				throw new ArgumentNullException(nameof(options));

			KafkaOptions = options.Value;

			var config = new ProducerConfig
			{
				BootstrapServers = KafkaOptions.BootstrapServerUrl,
				ClientId = KafkaOptions.ClientId,
			};

			_producer = new ProducerBuilder<KafkaMessageId, SampleMessage>(config).Build();
		}

		public KafkaOptions KafkaOptions { get; }

		public Task<DeliveryResult<KafkaMessageId, SampleMessage>> ProduceAsync(SampleMessage message, CancellationToken token)
		{
			var kafkaMessage = new Message<KafkaMessageId, SampleMessage>()
			{
				Key = new KafkaMessageId(message.SessionId, message.WithinSessionMessageId),
				Value = message
			};
			return _producer.ProduceAsync(KafkaOptions.TopicName, kafkaMessage, token);
		}

		public void Dispose()
		{
			if (_producer != null)
			{
				_producer.Dispose();
				_producer = null;
			}
		}
	}
}
