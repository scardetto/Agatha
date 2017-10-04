using System;
using System.Collections.Generic;
using System.Linq;

namespace Agatha.Common
{
    public class AsyncRequestDispatcherStub : Disposable, IAsyncRequestDispatcher
    {
        private readonly Dictionary<Type, string> _unkeyedTypesToAutoKey;
        private readonly Dictionary<string, Request> _requests;
        private readonly Dictionary<string, int> _responseKeyToIndexPosition;
        private readonly List<Response> _responsesToReturn;
        private ResponseReceiver _responseReceiver;

        public AsyncRequestDispatcherStub()
        {
            _unkeyedTypesToAutoKey = new Dictionary<Type, string>();
            _requests = new Dictionary<string, Request>();
            _responseKeyToIndexPosition = new Dictionary<string, int>();
            _responsesToReturn = new List<Response>();
        }

        public void SetResponsesToReturn(params Response[] responses)
        {
            _responsesToReturn.Clear();
            _responsesToReturn.AddRange(responses);
        }

        public void AddResponseToReturn(Response response, string key)
        {
            _responsesToReturn.Add(response);
            _responseKeyToIndexPosition.Add(key, _responsesToReturn.Count - 1);
        }

        public bool HasRequest<TRequest>() where TRequest : Request
        {
            return _unkeyedTypesToAutoKey.ContainsKey(typeof(TRequest));
        }

        public bool HasRequest<TRequest>(string key) where TRequest : Request
        {
            return _requests.ContainsKey(key) && (_requests[key] is TRequest);
        }

        public TRequest GetRequest<TRequest>() where TRequest : Request
        {
            var autoKey = _unkeyedTypesToAutoKey[typeof(TRequest)];
            return (TRequest)_requests[autoKey];
        }

        public TRequest GetRequest<TRequest>(string key) where TRequest : Request
        {
            return (TRequest)_requests[key];
        }

        public void ClearRequests()
        {
            _unkeyedTypesToAutoKey.Clear();
            _requests.Clear();
        }

        public void Add(Request request)
        {
            var autoKey = Guid.NewGuid().ToString();
            _unkeyedTypesToAutoKey.Add(request.GetType(), autoKey);
            _requests.Add(autoKey, request);
        }

        public void Add(params Request[] requestsToAdd)
        {
            if (requestsToAdd != null)
            {
                foreach (var request in requestsToAdd)
                {
                    Add(request);
                }
            }
        }

        public void Add<TRequest>(Action<TRequest> action) where TRequest : Request, new()
        {
            var request = new TRequest();
            action(request);
            Add(request);
        }

        public void Add(string key, Request request)
        {
            _requests.Add(key, request);
        }

        public void ProcessOneWayRequests()
        {
            //wanted to send it to the RequestProcessor, but none available..
        }

        public void ProcessRequests(Action<ReceivedResponses> receivedResponsesDelegate, Action<ExceptionInfo> exceptionOccurredDelegate)
        {
            ProcessRequests(new ResponseReceiver(receivedResponsesDelegate, exceptionOccurredDelegate, _responseKeyToIndexPosition, null));
        }

        public void ProcessRequests(Action<ReceivedResponses> receivedResponsesDelegate, Action<ExceptionInfo, ExceptionType> exceptionAndTypeOccurredDelegate)
        {
            ProcessRequests(new ResponseReceiver(receivedResponsesDelegate, exceptionAndTypeOccurredDelegate, _responseKeyToIndexPosition, null));
        }

        private void ProcessRequests(ResponseReceiver responseReceiver)
        {
            _responseReceiver = responseReceiver;
        }

        public void ReturnResponses()
        {
            _responseReceiver.ReceiveResponses(new ProcessRequestsAsyncCompletedArgs(new object[] { _responsesToReturn.ToArray() }, null, false, null), new Response[_responsesToReturn.Count], _requests.Values.ToArray());
        }

        protected override void DisposeManagedResources() { }
    }
}