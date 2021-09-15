using System.Collections.Generic;
using System.Threading.Tasks;
using Web.MessagingModels.Interfaces;

namespace Web.Services.Interfaces
{
	public interface IRestSender<T>: IHasRestTalkOptions where T: IMessagingModel
	{		
		Task PostBatch(IEnumerable<T> messages);
		Task AddToBatch(T message);
	}
}
