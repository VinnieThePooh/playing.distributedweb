using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Web.MessagingModels.Models;
using Web.Services.Kafka.Consumers;

namespace Web.HostedServices.Interfaces
{
	public interface IKafkaConsumerService: IHostedService
	{
		ISampleMessageConsumer<Ignore> SampleMessageConsumer { get; }
	}
}
