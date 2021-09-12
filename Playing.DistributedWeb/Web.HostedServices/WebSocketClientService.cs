﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Web.MessagingModels;
using Web.MessagingModels.Options;
using System.Net.WebSockets;
using Web.HostedServices.Interfaces;

namespace Web.HostedServices
{
	public class WebSocketClientService : BackgroundService, IWebSocketClientService
	{
		private static Random _random = new Random();
		private ClientWebSocket _clientWebSocket;
		private CancellationTokenSource _stoppingMessagingCts;
		private ManualResetEvent _manualReset = new ManualResetEvent(false);

		public WebSocketClientService(IOptions<MessagingOptions> options, IOptions<WebSocketConnectionOptions> connectionOptions)
		{
			MessagingOptions = options.Value;
		}

		public MessagingOptions MessagingOptions { get; private set; }

		public WebSocketConnectionOptions ConnectionOptions { get; private set; }

		public bool IsMessaging => _stoppingMessagingCts != null;


		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Task.Yield();

			while (stoppingToken.IsCancellationRequested)
			{
				try
				{
					_manualReset.WaitOne();
					await StartMessagingSession(_stoppingMessagingCts.Token);

					if (MessagingOptions.PauseBettweenMessages > 0)
						await Task.Delay(MessagingOptions.PauseBettweenMessages);
				}
				catch (OperationCanceledException ce)
				{
					//do nothing
				}
				catch (Exception e)
				{
					//do log here						
				}
			}
		}

		private async Task StartMessagingSession(CancellationToken stopMessagingToken)
		{
			var workingTask = Task.Run(() => DoMessaging(stopMessagingToken), stopMessagingToken);
			await Task.Delay(MessagingOptions.Duration * 1000);
			await StopMessaging();
			await workingTask;
		}


		public async Task<bool> StartMessaging()
		{
			if (_stoppingMessagingCts != null)
				return false;

			_stoppingMessagingCts = new CancellationTokenSource();
			_manualReset.Set();
			return true;
		}

		public async Task<bool> StopMessaging()
		{
			if (_stoppingMessagingCts == null)
				return false;

			_manualReset.Reset();
			_stoppingMessagingCts.Cancel();			
			_stoppingMessagingCts.Dispose();
			_stoppingMessagingCts = null;			
			return true;
		}
		

		private Task DoMessaging(CancellationToken stopMessagingToken)
		{
			while (true)
			{
				stopMessagingToken.ThrowIfCancellationRequested();

				// main work is gonna be done here

			}
		}



		private Task SendMessage(SampleMessage message)
		{
			//var messageId = 0;
			//var sessionId = _random.Next(1);

			//create message
			//var sample = new SampleMessage
			//{
			//	SessionId = sessionId,
			//	Id = ++messageId,
			//	NodeOne_Timestamp = DateTime.Now
			//};

			return Task.CompletedTask;	
		}
	}
}
