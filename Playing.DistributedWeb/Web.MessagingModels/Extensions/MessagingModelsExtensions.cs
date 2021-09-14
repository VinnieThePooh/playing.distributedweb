using System.Text.Json;
using Web.MessagingModels.Interfaces;

namespace Web.MessagingModels.Extensions
{
	public static class MessagingModelsExtensions
	{
		public static string ToJson(this IMessagingModel model)
		{
			if (model == null)
				return "NULL";

			//reflection?
			return JsonSerializer.Serialize(model, model.GetType());
		}
	}
}
