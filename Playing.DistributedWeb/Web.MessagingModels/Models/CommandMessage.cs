using Web.MessagingModels.Interfaces;

namespace Web.MessagingModels.Models
{
	public class CommandMessage: IMessagingModel
	{
		//static int JsonSerializedObjectLength  

		public CommandCode CommandCode { get; set; }
	}
}
