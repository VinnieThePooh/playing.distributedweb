using Confluent.Kafka;
using System.Collections.Generic;
using System.Threading;
using Web.MessagingModels;

namespace Web.Services.Kafka.Consumers
{
	public interface ISampleMessageConsumer<TKey>
	{
		ConsumeResult<TKey, SampleMessage> Consume(CancellationToken token);

		IAsyncEnumerable<ConsumeResult<TKey, SampleMessage>> ConsumeRange(CancellationToken token);
	}
}
