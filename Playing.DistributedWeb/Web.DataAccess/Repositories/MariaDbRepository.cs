using System;
using System.Threading.Tasks;
using Web.DataAccess.Interfaces;
using Web.MessagingModels;

namespace Web.DataAccess.Repositories
{
	public class MariaDbRepository : ISampleMessageRepository
	{
		public MariaDbRepository(string connectionString)
		{

		}

		public Task<int> GetLastSessionId()
		{

			return Task.FromResult(int.MinValue);
		}

		public Task InsertNewMessage(SampleMessage newMessage)
		{
			if (newMessage is null)			
				throw new ArgumentNullException(nameof(newMessage));

			return Task.CompletedTask;
		}
	}
}
