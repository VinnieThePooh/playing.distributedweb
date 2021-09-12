using System.Threading.Tasks;
using Web.MessagingModels.Options;

namespace Web.HostedServices.Interfaces
{
	public interface IWebSocketClientService
	{
		Task<bool> StartMessaging();
		Task<bool> StopMessaging();

		MessagingOptions MessagingOptions { get; }

		WebSocketConnectionOptions ConnectionOptions { get; }

		bool IsMessaging { get; }
	}
}
