using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Web.HostedServices.Interfaces;

namespace Web.NodeOne.Controllers
{
	[Route("/")]
	[ApiController]
	public class MessagingController : ControllerBase
	{
		private readonly IWebSocketClientService _webSocketClient;

		public MessagingController(IWebSocketClientService webSocketClient)
		{
			_webSocketClient = webSocketClient;
		}

		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[HttpGet("start")]		
		public async Task<IActionResult> StartMessaging()
		{
			//todo: return Ok only after successful connect or after continuing sending
			if (!await _webSocketClient.StartMessaging())
				return BadRequest("WebSocketClientService is already busy now");

			return Ok("Messaging started");
		}


		[HttpGet("stop")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> StopMessaging()
		{
			if (!await _webSocketClient.StopMessaging())
				return BadRequest("WebSocketClientService is already stopped");

			return Ok("Messaging stopped");
		}
	}
}
