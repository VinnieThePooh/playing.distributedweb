using System.Threading;

namespace Web.Services.Interfaces
{
	public interface IHasCancellationToken
	{
		CancellationToken Token { get; }
	}
}
