namespace Web.MessagingModels.Options
{
	public class KafkaProducerOptions
	{
		public string ClientId { get; set; }

		public string BootstrapServerUrl { get; set; }

		public string TopicName { get; set; }
	} 
}
