namespace Web.MessagingModels.Constants
{
	public static class OperationNames
	{
		public const string DB_PERSIST = "DB.Persisting";
		// web socket data receive
		public const string WS_DATA_RECEIVE = "WS.Receiving";

		//kafka interaction is asynchronous - so, operation name with -ed suffix, not -ing
		public const string KAFKA_MESSAGE_CONSUMED = "KAFKA.Consumed";

		public const string HTTP_BATCH_SENT = "HTTP.BatchSent";

		//normal cancelling
		public const string WORK_CANCELLED = "WORK.Cancelled";

		//work failed due to an error
		public const string WORK_FAILED = "WORK.Failed";
	}
}
