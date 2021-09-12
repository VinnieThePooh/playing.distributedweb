using System.Threading.Tasks;
using Web.MessagingModels;

namespace Web.DataAccess.Interfaces
{
	public interface ISampleMessageRepository
	{
		Task InsertNewMessage(SampleMessage newMessage);

		Task<int> GetLastSessionId();
	}
}
