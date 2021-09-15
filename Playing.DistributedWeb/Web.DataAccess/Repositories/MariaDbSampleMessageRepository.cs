using System;
using System.Threading.Tasks;
using Web.DataAccess.Interfaces;
using Web.MessagingModels;
using MySqlConnector;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

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

		public async Task InsertBatch(IEnumerable<SampleMessage> messages)
		{
			if (messages is null)
				throw new ArgumentNullException(nameof(messages));

			var builder = new StringBuilder(@"INSERT INTO sample_messages
											  (SessionId, WithinSessionMessageId, NodeOne_Timestamp, NodeTwo_Timestamp, NodeThree_Timestamp, End_Timestamp) values");

			var count  = messages.Count();
			var counter = 1;
			foreach (var message in messages)
			{
				builder.AppendLine(GetInsertFragment(message, counter++ == count));
			}

			var sql = builder.ToString();
			using var connection = new MySqlConnection(_connectionString);
			var command = connection.CreateCommand();			
			command.CommandText = sql;
			await connection.OpenAsync();
			await command.ExecuteNonQueryAsync();
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
		
		private string GetInsertFragment(SampleMessage message, bool isLastMessage)
		{			
			var cinfo = CultureInfo.InvariantCulture;
			return $@"({message.SessionId},
					   {message.WithinSessionMessageId},
					   '{(message.NodeOne_Timestamp.HasValue ? message.NodeOne_Timestamp.Value.ToString("s" , cinfo): null)}',
					   '{(message.NodeTwo_Timestamp.HasValue ? message.NodeTwo_Timestamp.Value.ToString("s", cinfo) : null)}',
					   '{(message.NodeThree_Timestamp.HasValue ? message.NodeThree_Timestamp.Value.ToString("s", cinfo) : null)}',
					   '{(message.End_Timestamp.HasValue ? message.End_Timestamp.Value.ToString("s", cinfo) : null)}'){(isLastMessage ? ";": ",")}";
		}
	}
}
