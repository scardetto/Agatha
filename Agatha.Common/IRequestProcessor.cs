using System;
using System.Threading.Tasks;

namespace Agatha.Common
{
	public interface IRequestProcessor : IDisposable
	{
		Response[] Process(params Request[] requests);
		Task<Response[]> ProcessAsync(params Request[] requests);
	}
}