namespace Web.MessagingModels.Options
{
	public class KafkaOptions
	{
		public string ClientId { get; set; }

		public string BootstrapServerUrl { get; set; }

		public string TopicName { get; set; }
	} 
}
