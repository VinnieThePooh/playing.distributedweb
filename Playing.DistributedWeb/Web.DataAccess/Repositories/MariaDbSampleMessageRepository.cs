using System;
using System.Threading.Tasks;
using Web.DataAccess.Interfaces;
using Web.MessagingModels;
using MySqlConnector;
using System.Collections.Generic;

namespace Web.DataAccess.Repositories
{
	public class MariaDbSampleMessageRepository : ISampleMessageRepository
	{
		public static int LastSessionId { get; private set; }

		private readonly string _connectionString;

		public MariaDbSampleMessageRepository(string connectionString)
		{
			_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		public async Task<int> GetLastSessionId()
		{
			using (var con = new MySqlConnection(_connectionString))
			{
				var command = con.CreateCommand();
				command.CommandText = "select max(SessionId) from sample_messages;";
				await con.OpenAsync();
				var lastId = await command.ExecuteScalarAsync();
				return  lastId != DBNull.Value ? (int)lastId: 0;
			}			
		}

		public Task InsertNewMessage(SampleMessage newMessage)
		{
			if (newMessage is null)
				throw new ArgumentNullException(nameof(newMessage));

			return Task.CompletedTask;
		}

		public Task InsertBatch(IEnumerable<SampleMessage> messages)
		{
			throw new NotImplementedException();
		}

		public static Task SetLastSessionId(int sessionId)
		{
			LastSessionId = sessionId;
			return Task.CompletedTask;
		}

		public Task<int> GetCachedLastSessionId() => Task.FromResult(LastSessionId);

		public Task SetCachedLastSessionId(int newSessionId)
		{
			SetLastSessionId(newSessionId);
			return Task.CompletedTask;
		}		
	}
}
