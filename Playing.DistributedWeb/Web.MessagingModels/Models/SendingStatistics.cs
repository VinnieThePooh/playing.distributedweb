namespace Web.MessagingModels.Models
{
	public struct SendingStatistics
	{
		public SendingStatistics(int formalDuration, long actualDuration, int messagesHandled)
		{
			FormalDuration = formalDuration;
			ActualDuration = actualDuration;
			MessagesHandled = messagesHandled;
			GracefulCloseInterval = 0;
		}

		public SendingStatistics(int formalDuration):this(formalDuration, 0, 0)
		{ }

		//in seconds
		public int FormalDuration { get; set; }

		//in ms
		public long ActualDuration { get; set; }

		public int MessagesHandled { get; set; }

		//in ms
		public long GracefulCloseInterval { get; set; }

		// from beginning to close confirmation
		public long TotalSessionTime => GracefulCloseInterval + ActualDuration;
	}
}
