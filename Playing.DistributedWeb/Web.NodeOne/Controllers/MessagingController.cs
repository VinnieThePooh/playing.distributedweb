using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Web.NodeOne.Controllers
{
	[Route("/")]
	[ApiController]
	public class MessagingController : ControllerBase
	{		
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[HttpGet("start")]		
		public IActionResult StartMessaging()
		{
			return Ok("Messaging started");
		}


		[HttpGet("stop")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public IActionResult StopMessaging()
		{
			return Ok("Messaging stopped");
		}
	}
}
