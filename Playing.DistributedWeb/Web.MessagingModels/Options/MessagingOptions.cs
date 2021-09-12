namespace Web.MessagingModels.Options
{
	public class MessagingOptions
	{
		//messaging interval - in seconds
		public int Duration { get; set; }

		// in ms
		public int PauseBettweenMessages { get; set; }
	}
}
