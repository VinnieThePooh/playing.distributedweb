using System.Threading.Tasks;

namespace Web.DataAccess.Interfaces
{
	public interface ICachedLastSessionIdProvider
	{
		Task<int> GetCachedLastSessionId();
		Task SetCachedLastSessionId(int newSessionId);
	}
}
