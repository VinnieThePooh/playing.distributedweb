using Confluent.Kafka;
using ProtoBuf;
using System.IO;

namespace Web.Services.Kafka.Serialization
{
	public class CustomSerializer<T> : ISerializer<T>
	{	
		public byte[] Serialize(T data, Confluent.Kafka.SerializationContext context)
		{
			if (data == null)
				return new byte[0];

			using var memory = new MemoryStream();
			Serializer.Serialize(memory, data);
			return memory.ToArray();
		}
	}
}
