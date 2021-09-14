using ProtoBuf;
using System;
using Web.MessagingModels.Interfaces;

namespace Web.MessagingModels
{
	[ProtoContract]
	public class SampleMessage: IMessagingModel
	{
		public const string TableName = "sample_messages";

		// db pk
		
		[ProtoMember(1)]
		public int Id { get; set; }

		[ProtoMember(2)]
		public int SessionId { get; set; }

		//to track within session		
		[ProtoMember(3)]
		public int WithinSessionMessageId { get; set; }

		[ProtoMember(4)]
		public DateTime? NodeOne_Timestamp { get; set; }

		[ProtoMember(5)]
		public DateTime? NodeTwo_Timestamp { get; set; }

		[ProtoMember(6)]
		public DateTime? NodeThree_Timestamp { get; set; }

		[ProtoMember(7)]
		public DateTime? End_Timestamp { get; set; }

		public override string ToString()
		{
			return $"[SessionId: {SessionId}; WithinSessionMessageId: {WithinSessionMessageId}]";
		}
	}
}
