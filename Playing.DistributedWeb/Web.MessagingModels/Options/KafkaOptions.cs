namespace Web.MessagingModels.Options
{
	public class KafkaOptions
	{
		public string ClientId { get; set; }

		public string BootstrapServerUrl { get; set; }

		public string TopicName { get; set; }
		
		//producer related
		public bool ProduceToKafka { get; set; }

		//producer relaed
		public bool ConsumeFromKafka { get; set; }
	} 
}
