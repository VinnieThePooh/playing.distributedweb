using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Web.HostedServices.Models;
using Web.MessagingModels.Options;

namespace Web.HostedServices.Interfaces
{
	public interface IWebSocketClientService: IHostedService
	{
		ValueTask<OperResult> StartMessaging();
		ValueTask<OperResult> StopMessaging();

		MessagingOptions MessagingOptions { get; }

		WebSocketConnectionOptions ConnectionOptions { get; }

		ServiceState ServiceState { get; }
	}
}
