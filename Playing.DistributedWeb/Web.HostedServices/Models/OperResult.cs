namespace Web.HostedServices.Models
{
	public struct OperResult
	{
		public OperResult(string message)
		{
			ResultMessage = message;
		}

		public bool IsSucceeded => ResultMessage is null;

		public string ResultMessage { get; }

		public static OperResult Succeeded => new OperResult();
		
		public static class Messages
		{
			public const string WaitingForGracefulClose = "Waiting for graceful close";

			public const string SendingData = "Still sending data";

			public const string AlreadyStopped = "Service already stopped";
		}
	}
}
