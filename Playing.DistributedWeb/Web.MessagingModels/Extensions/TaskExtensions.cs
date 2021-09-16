using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Web.MessagingModels.Extensions.Tasks
{
	public static class TaskExtensions
	{
		public static Task WithStopwatchRacing(this Task task, TimeSpan interval)
		{
			var sw = Stopwatch.StartNew();
			var ms = interval.TotalMilliseconds;
			var swBased = Task.Factory.StartNew(() => {			
				while (sw.ElapsedMilliseconds < ms)
				{
					Thread.Sleep(10);
				}
			}, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);

			return Task.WhenAny(task, swBased);
		}
	}
}
