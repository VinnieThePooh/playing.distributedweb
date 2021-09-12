using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Web.MessagingModels.Options;

namespace Web.HostedServices.Interfaces
{
	public interface IWebSocketClientService: IHostedService
	{
		Task<bool> StartMessaging();
		Task<bool> StopMessaging();

		MessagingOptions MessagingOptions { get; }

		WebSocketConnectionOptions ConnectionOptions { get; }

		bool IsMessaging { get; }
	}
}
