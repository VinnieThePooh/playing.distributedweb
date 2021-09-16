namespace Web.MessagingModels.Models
{
	public struct SendingStatistics
	{
		public SendingStatistics(int formalDuration, long actualDuration, int messagesHandled)
		{
			FormalDuration = formalDuration;
			ActualDuration = actualDuration;
			MessagesHandled = messagesHandled;
		}

		public SendingStatistics(int formalDuration):this(formalDuration, 0, 0)
		{ }

		//in seconds
		public int? FormalDuration { get; set; }

		//in ms
		public long ActualDuration { get; set; }

		public int MessagesHandled { get; set; }
	}
}
