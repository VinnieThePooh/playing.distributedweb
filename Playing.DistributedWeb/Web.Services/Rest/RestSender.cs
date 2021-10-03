using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Web.MessagingModels;
using Web.MessagingModels.Options;
using Web.Services.Interfaces;

namespace Web.Services.Rest
{
	public class RestSender : IRestSender<SampleMessage>, IDisposable
	{
		private List<SampleMessage> _internalBatch;

		private Lazy<HttpClient> _lazyClient;
		private HttpClient _httpClient => _lazyClient.Value;

		private bool _wasDisposed;

		public RestSender(IOptions<RestTalkOptions> options)
		{
			TalkOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));

			if (string.IsNullOrEmpty(TalkOptions.EndPointUrl))
				throw new ArgumentException("Endpoint url is not set for RestSender");

			_internalBatch = new List<SampleMessage>();
			_lazyClient = new Lazy<HttpClient>(() => {

				var handler = new HttpClientHandler();
				handler.ServerCertificateCustomValidationCallback += SslValidationCallback;

				var client = new HttpClient(handler);
				client.BaseAddress = new Uri(TalkOptions.EndPointUrl);				
				return client;
			});
		}

		public RestTalkOptions TalkOptions { get; }

		/// <summary>
		/// Adds message to internal batch and post data if batch size is reached
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public async Task AddToBatch(SampleMessage message)
		{
			if (_wasDisposed)
				throw new ObjectDisposedException(nameof(RestSender));

			if (message is null)
				throw new ArgumentNullException(nameof(message));		

			_internalBatch.Add(message);

			if (_internalBatch.Count % TalkOptions.BatchSize == 0)
			{
				await PostData(_internalBatch);
				Debug.WriteLine($"Sent batch({TalkOptions.BatchSize} models) to Web.NodeOne");
				_internalBatch.Clear();
			}
		}		

		public async Task PostBatch(IEnumerable<SampleMessage> messages)
		{
			if (messages is null)			
				throw new ArgumentNullException(nameof(messages));			

			if (_wasDisposed)
				throw new ObjectDisposedException(nameof(RestSender));			

			await PostData(messages);
		}		

		public void Dispose()
		{
			if (_wasDisposed)
				return;

			_httpClient?.Dispose();
			_internalBatch.Clear();
			_internalBatch = null;
			ServicePointManager.ServerCertificateValidationCallback -= SslValidationCallback;
			_wasDisposed = true;
		}

		private async Task PostData(IEnumerable<SampleMessage> messages)
		{
			//todo: configure it later for effective reusing
			var content = JsonContent.Create(messages);
			var result = await _httpClient.PostAsync((Uri?)null, content);
			result.EnsureSuccessStatusCode();
		}

		private bool SslValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;
		
	}
}
