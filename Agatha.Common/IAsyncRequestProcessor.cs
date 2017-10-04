using System;

namespace Agatha.Common
{
	public interface IAsyncRequestProcessor : IDisposable
	{
		IAsyncResult BeginProcessRequests(Request[] requests, AsyncCallback callback, object asyncState);
		Response[] EndProcessRequests(IAsyncResult result);
		void ProcessRequestsAsync(Request[] requests, Action<ProcessRequestsAsyncCompletedArgs> processCompleted);
	}
}