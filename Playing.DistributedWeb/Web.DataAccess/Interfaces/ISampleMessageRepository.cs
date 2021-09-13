using System.Threading.Tasks;
using Web.MessagingModels;

namespace Web.DataAccess.Interfaces
{
	public interface ISampleMessageRepository: ICachedLastSessionIdProvider
	{
		Task InsertNewMessage(SampleMessage newMessage);

		Task<int> GetLastSessionId();
	}
}
