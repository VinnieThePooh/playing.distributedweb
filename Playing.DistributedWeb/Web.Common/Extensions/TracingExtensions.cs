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
		/// <param name="tagName">Web-service name</param>
		/// <param name="tagValue">Actual name of event happened</param>
		public static void AuditEvent(this ITracer tracer, string operationName, string tagName, string tagValue)
		{
			ThrowIfIncorrectValues(operationName, tagName, tagValue);
			using var scope = tracer
				.BuildSpan(operationName)
				.WithTag(tagName, tagValue)
				.WithStartTimestamp(DateTimeOffset.Now).StartActive();
		}

		public static ISpan StartAuditActivity(this ITracer tracer, string operationName, string tagName,
			string tagValue)
		{
			ThrowIfIncorrectValues(operationName, tagName, tagValue);
			return tracer
					.BuildSpan(operationName)
					.WithTag(tagName, tagValue)
					.WithStartTimestamp(DateTimeOffset.Now).Start();
		}

		private static void ThrowIfIncorrectValues(string opName, string tagName, string tagValue)
		{
			if (string.IsNullOrEmpty(opName))
				throw new ArgumentException(nameof(opName));

			if (string.IsNullOrEmpty(tagName))
				throw new ArgumentException(nameof(tagName));

			if (string.IsNullOrEmpty(tagValue))
				throw new ArgumentException(nameof(tagValue));
		}
	}
}
