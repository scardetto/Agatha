using System;
using System.Threading;
using Agatha.Common;

namespace Agatha.ServiceLayer
{
	public class AsyncRequestProcessor : Disposable, IAsyncRequestProcessor
	{
		private readonly IRequestProcessor _requestProcessor;

		private readonly Func<Request[], Response[]> _processFunc;

		public AsyncRequestProcessor(IRequestProcessor requestProcessor)
		{
			_requestProcessor = requestProcessor;
			_processFunc = requestProcessor.Process;
		}

		protected override void DisposeManagedResources()
		{
		    _requestProcessor?.Dispose();
		}

		public IAsyncResult BeginProcessRequests(Request[] requests, AsyncCallback callback, object asyncState)
		{
			return _processFunc.BeginInvoke(requests, callback, asyncState);
		}

		public Response[] EndProcessRequests(IAsyncResult result)
		{
			return _processFunc.EndInvoke(result);
		}

		public void ProcessRequestsAsync(Request[] requests, Action<ProcessRequestsAsyncCompletedArgs> callback)
		{
			var asyncResult = BeginProcessRequests(requests, null, null);
			ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle,
				(state, timedout) => ProcessRequestsCompleted((IAsyncResult)state, callback), asyncResult, -1, true);
		}

		private void ProcessRequestsCompleted(IAsyncResult asyncResult, Action<ProcessRequestsAsyncCompletedArgs> callback)
		{
			try
			{
				var responses = EndProcessRequests(asyncResult);
				callback(new ProcessRequestsAsyncCompletedArgs(new object[] { responses }, null, false, null));
			}
			catch (Exception e)
			{
				callback(new ProcessRequestsAsyncCompletedArgs(null, e, false, null));
			}
		}
	}
}