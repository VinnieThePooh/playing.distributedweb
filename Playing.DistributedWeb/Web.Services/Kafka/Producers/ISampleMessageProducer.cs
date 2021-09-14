using Confluent.Kafka;
using System.Threading;
using System.Threading.Tasks;
using Web.MessagingModels;

namespace Web.Services.Kafka.Producers
{
	public interface ISampleMessageProducer<TKey>
	{
		Task<DeliveryResult<TKey, SampleMessage>> ProduceAsync(SampleMessage message, CancellationToken token);
	}
}
