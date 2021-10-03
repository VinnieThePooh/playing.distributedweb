using System;
using OpenTracing;

namespace Web.Common.Extensions
{
	public static class TracingExtensions
	{
		/// <summary>
		/// Does audit event
		/// </summary>
		/// <param name="tracer"></param>
		/// <param name="operationName">Long-running process name or parent activity(span) name.</param>
		/// <param name="tagName">WebNode name</param>
		/// <param name="tagValue">Actual name of event happened</param>
		public static void AuditEvent(this ITracer tracer, string operationName, string tagName, string tagValue)
		{
			if (string.IsNullOrEmpty(operationName))
				throw new ArgumentException(nameof(operationName));

			if (string.IsNullOrEmpty(tagName))
				throw new ArgumentException(nameof(tagName));

			if (string.IsNullOrEmpty(tagValue))
				throw new ArgumentException(nameof(tagValue));

			using var scope = tracer
				.BuildSpan(operationName)
				.WithTag(tagName, tagValue)
				.WithStartTimestamp(DateTime.Now).StartActive();
		}
	}
}
