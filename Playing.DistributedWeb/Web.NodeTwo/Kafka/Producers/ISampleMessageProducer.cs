using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Web.MessagingModels;

namespace Web.NodeTwo.Kafka.Producers
{
	interface ISampleMessageProducer<TKey>
	{
		Task<DeliveryResult<TKey, SampleMessage>> ProduceAsync(SampleMessage message);
	}
}
