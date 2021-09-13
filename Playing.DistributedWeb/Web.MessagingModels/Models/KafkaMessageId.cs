namespace Web.MessagingModels.Models
{
	public struct KafkaMessageId
	{
		public KafkaMessageId(int sessionId, int withinSessionMessageId)
		{
			SessionId = sessionId;
			WithinSessionMessageId = withinSessionMessageId;
		}

		public int WithinSessionMessageId { get; }

		public int SessionId { get; }
	}
}
