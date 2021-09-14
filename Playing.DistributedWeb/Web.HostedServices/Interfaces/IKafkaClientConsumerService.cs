using Web.MessagingModels;
using Web.MessagingModels.Models;
using Web.Services.Kafka.Consumers;

namespace Web.HostedServices.Interfaces
{
	interface IKafkaClientConsumerService
	{
		ISampleMessageConsumer<KafkaMessageId> SampleMessageConsumer { get; }
	}
}
