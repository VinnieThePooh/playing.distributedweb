namespace Web.MessagingModels.Options
{
	public class WebSocketConnectionOptions
	{
		public string SocketUrl { get; set; }

		public int DisconnectRetryCount { get; set; }
	}
}
