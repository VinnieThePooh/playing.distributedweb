using System;

namespace Web.MessagingModels
{
	public class SampleMessage
	{
		public const string TableName = "sample_messages";

		public int Id { get; set; }

		public int SessionId { get; set; }

		public DateTime? NodeOne_Timestamp { get; set; }

		public DateTime? NodeTwo_Timestamp { get; set; }

		public DateTime? NodeThree_Timestamp { get; set; }

		public DateTime? End_Timestamp { get; set; }
	}
}
