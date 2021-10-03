namespace Web.MessagingModels.Constants
{
	public static class JaegerTagValues
	{
		public static class NodeOne
		{
			public const string BATCH_RECEIVED = "N1.BatchReceived:Persist";
		}

		public static class NodeTwo
		{
			public const string SOCKET_SERVER_MESSAGE_RECEIVED = "N2.WS:MessageReceived";
		}

		public static class NodeThree
		{
			public const string KAFKA_MESSAGE_CONSUMED = "N3.KafkaMessageConsumed";
		}
	}
}
