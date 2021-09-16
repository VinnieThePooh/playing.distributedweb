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

		//todo: instead of this add State as enum with three states:
		//1.Stopped
		//2.SendingData
		//3.WaitingForGracefulClose
		bool IsMessaging { get; }
	}
}
