namespace Web.MessagingModels.Options
{
	public class WebSocketServerOptions
	{
		public string AllowedOrigins { get; set; }
		public int KeepAliveInterval { get; set; }		
		public string SocketPath { get; set; }
	}
}
