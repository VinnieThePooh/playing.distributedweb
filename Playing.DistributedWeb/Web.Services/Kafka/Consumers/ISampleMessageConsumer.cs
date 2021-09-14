using Confluent.Kafka;
using System.Collections.Generic;
using System.Threading;
using Web.MessagingModels;
using Web.Services.Interfaces;

namespace Web.Services.Kafka.Consumers
{
	public interface ISampleMessageConsumer<TKey>: IHasCancellationToken
	{
		ConsumeResult<TKey, SampleMessage> Consume(CancellationToken token);

		IAsyncEnumerable<ConsumeResult<TKey, SampleMessage>> ConsumeRange(CancellationToken token);
	}
}
