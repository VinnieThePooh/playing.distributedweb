using Confluent.Kafka;
using ProtoBuf;
using System;

namespace Web.Services.Kafka.Serialization
{
	public class CustomDeserializer<T> : IDeserializer<T>
	{
		public T Deserialize(ReadOnlySpan<byte> data, bool isNull, Confluent.Kafka.SerializationContext context)
		{
			if (data.IsEmpty)
				return default;

			return Serializer.Deserialize<T>(data);
		}
	}
}
