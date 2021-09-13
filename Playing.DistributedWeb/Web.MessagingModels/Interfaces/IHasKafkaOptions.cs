using Web.MessagingModels.Options;

namespace Web.MessagingModels.Interfaces
{
	public interface IHasKafkaOptions
	{
		KafkaOptions KafkaOptions { get; }
	}
}
