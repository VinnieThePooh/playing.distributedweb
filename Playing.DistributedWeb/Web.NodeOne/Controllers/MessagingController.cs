using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTracing;
using System;
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
		private readonly ISampleMessageRepository _messagesRepository;
		private readonly ITracer _tracer;

		public MessagingController(IWebSocketClientService webSocketClient, ISampleMessageRepository messagesRepository, ITracer tracer)
		{
			_webSocketClient = webSocketClient ?? throw new System.ArgumentNullException(nameof(webSocketClient));
			_messagesRepository = messagesRepository ?? throw new System.ArgumentNullException(nameof(messagesRepository));
			_tracer = tracer;
		}

		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[HttpGet("start")]		
		public async Task<IActionResult> StartMessaging()
		{
			var result = await _webSocketClient.StartMessaging();
			if (!result.IsSucceeded)
			{
				var prefix = "[WebSocketClientService]";
				return BadRequest($"{prefix}: {result.ResultMessage}");
			}
			return Ok("Messaging started");
		}


		[HttpGet("stop")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> StopMessaging()
		{
			var res = await _webSocketClient.StopMessaging();
			if (!res.IsSucceeded)
			{
				var prefix = "[WebSocketClientService]";
				return BadRequest($"{prefix}: {res.ResultMessage}");
			}
			return Ok("Messaging stopped");
		}


		[HttpPost("end-roundtrip-batch")]
		public async Task<ActionResult> AcceptMessages(IEnumerable<SampleMessage> messages)
		{
			var now = DateTime.Now;
			var span = _tracer.BuildSpan("end-roundtrip-batch").WithStartTimestamp(now);
			
			foreach (var message in messages)
				message.End_Timestamp = now;

			var started = span.Start();	
			await _messagesRepository.InsertBatch(messages);
			Debug.WriteLine($"Received data batch ({messages.Count()} of SampleMessage entity) and successfully persisted it to MariaDb");
			started.Finish(DateTimeOffset.Now);
			return Accepted();
		}
	}
}
