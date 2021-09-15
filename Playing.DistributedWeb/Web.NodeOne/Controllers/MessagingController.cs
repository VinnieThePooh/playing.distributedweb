using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Web.DataAccess.Interfaces;
using Web.HostedServices.Interfaces;
using Web.MessagingModels;

namespace Web.NodeOne.Controllers
{
	[Route("/")]
	[ApiController]
	public class MessagingController : ControllerBase
	{
		private readonly IWebSocketClientService _webSocketClient;
		private readonly ISampleMessageRepository _messageRepository;

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


		[HttpPost("end-roundtrip")]
		public async Task<ActionResult> AcceptMessages(IEnumerable<SampleMessage> messages)
		{
			Debug.WriteLine($"Received {messages.Count()} messages");
			return Accepted();
		}
	}
}
