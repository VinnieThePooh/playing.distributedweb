namespace Web.HostedServices.Models
{
	public enum ServiceState
	{
		Stopped,
		SendingData,
		WaitingForGracefulClose
	}
}
