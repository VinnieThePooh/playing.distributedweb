using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Web.MessagingModels;
using Web.MessagingModels.Interfaces;
using Web.MessagingModels.Models;
using Web.MessagingModels.Options;

namespace Web.NodeTwo.Kafka.Producers
{
	public class SampleMessageProducer: ISampleMessageProducer<KafkaMessageId>, IHasKafkaOptions
	{
		public SampleMessageProducer(IOptions<KafkaOptions> options)
		{
			if (options is null)			
				throw new ArgumentNullException(nameof(options));			

			KafkaOptions = options.Value;			
		}

		public KafkaOptions KafkaOptions { get; }		

		public Task<DeliveryResult<KafkaMessageId, SampleMessage>> ProduceAsync(SampleMessage message)
		{
			throw new System.NotImplementedException();
		}
	}
}
