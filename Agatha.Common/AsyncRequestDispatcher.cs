using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agatha.Common.Caching;

namespace Agatha.Common
{
    public interface IAsyncRequestDispatcher : IDisposable
    {
        void Add(Request request);
        void Add(params Request[] requestsToAdd);
        void Add(string key, Request request);
        void Add<TRequest>(Action<TRequest> action) where TRequest : Request, new();

        Task<IEnumerable<Response>> GetResponses();
        Task<bool> HasResponse<TResponse>() where TResponse : Response;

        Task<TResponse> GetAsync<TResponse>() where TResponse : Response;
        Task<TResponse> GetAsync<TResponse>(string key) where TResponse : Response;
        Task<TResponse> GetAsync<TResponse>(Request request) where TResponse : Response;

        void Clear();
    }

    public class AsyncRequestDispatcher : Disposable, IAsyncRequestDispatcher
    {
        private readonly IRequestProcessor _requestProcessor;
        private readonly ICacheManager _cacheManager;

        private Dictionary<string, Type> _keyToTypes;
        private List<Request> _requests;
        private Response[] _responses;

        protected Dictionary<string, int> KeyToResultPositions;

        public AsyncRequestDispatcher(IRequestProcessor requestProcessor, ICacheManager cacheManager)
        {
            _requestProcessor = requestProcessor;
            _cacheManager = cacheManager;
            InitializeState();
        }

        private void InitializeState()
        {
            _requests = new List<Request>();
            _responses = null;
            _keyToTypes = new Dictionary<string, Type>();
            KeyToResultPositions = new Dictionary<string, int>();
        }

        public IEnumerable<Request> SentRequests => _requests;

        public async Task<IEnumerable<Response>> GetResponses()
        {
            await SendRequestsIfNecessary();
            return _responses;
        }

        public virtual void Add(params Request[] requestsToAdd)
        {
            foreach (var request in requestsToAdd)
            {
                Add(request);
            }
        }

        public virtual void Add<TRequest>(Action<TRequest> action) where TRequest : Request, new()
        {
            var request = new TRequest();
            action(request);
            Add(request);
        }

        public virtual void Add(Request request)
        {
            AddRequest(request, false);
        }

        public virtual void Add(string key, Request request)
        {
            if (_keyToTypes.Keys.Contains(key)) {
                throw new InvalidOperationException(
                    $"A request has already been added using the key '{key}'.");
            }

            _keyToTypes[key] = request.GetType();
            AddRequest(request, true);
            KeyToResultPositions[key] = _requests.Count - 1;
        }

        public virtual async Task<bool> HasResponse<TResponse>() where TResponse : Response
        {
            await SendRequestsIfNecessary();
            return _responses.OfType<TResponse>().Any();
        }

        private async Task<bool> HasResponse(string key)
        {
            await SendRequestsIfNecessary();
            return KeyToResultPositions.ContainsKey(key);
        }

        private async Task<bool> HasMoreThanOneResponse<TResponse>() where TResponse : Response
        {
            await SendRequestsIfNecessary();
            return _responses.OfType<TResponse>().Count() > 1;
        }

        public virtual async Task<TResponse> GetAsync<TResponse>() where TResponse : Response
        {
            await SendRequestsIfNecessary();

            if (!await HasResponse<TResponse>())
            {
                throw new InvalidOperationException(
                    $"There is no response with type {typeof(TResponse).FullName}. Maybe you called Clear before or forgot to add appropriate request first.");
            }

            if (await HasMoreThanOneResponse<TResponse>())
            {
                throw new InvalidOperationException(
                    $"There is more than one response with type {typeof(TResponse).FullName}. If two request handlers return responses with the same type, you need to add requests using Add(string key, Request request).");
            }

            return _responses.OfType<TResponse>().Single();
        }

        public virtual async Task<TResponse> GetAsync<TResponse>(string key) where TResponse : Response
        {
            await SendRequestsIfNecessary();
            if (!await HasResponse(key))
            {
                throw new InvalidOperationException(
                    $"There is no response with key '{key}'. Maybe you called Clear before or forgot to add appropriate request first.");
            }

            return (TResponse)_responses[KeyToResultPositions[key]];
        }

        public virtual async Task<TResponse> GetAsync<TResponse>(Request request) where TResponse : Response
        {
            Add(request);
            return await GetAsync<TResponse>();
        }

        public TResponse Get<TResponse>() where TResponse : Response
        {
            return GetAsync<TResponse>().GetAwaiter().GetResult();
        }

        public TResponse Get<TResponse>(string key) where TResponse : Response
        {
            return GetAsync<TResponse>(key).GetAwaiter().GetResult();
        }

        public TResponse Get<TResponse>(Request request) where TResponse : Response
        {
            return GetAsync<TResponse>(request).GetAwaiter().GetResult();
        }

        public virtual void Clear()
        {
            InitializeState();
        }

        protected override void DisposeManagedResources()
        {
            _requestProcessor?.Dispose();
        }

        protected virtual async Task<Response[]> GetResponses(params Request[] requestsToProcess)
        {
            BeforeSendingRequests(requestsToProcess);

            var tempResponseArray = new Response[requestsToProcess.Length];
            var requestsToSend = new List<Request>(requestsToProcess);

            GetCachedResponsesAndRemoveThoseRequests(requestsToProcess, tempResponseArray, requestsToSend);
            var requestsToSendAsArray = requestsToSend.ToArray();

            if (requestsToSend.Count > 0)
            {
                var receivedResponses = await _requestProcessor.ProcessAsync(requestsToSendAsArray);
                AddCacheableResponsesToCache(receivedResponses, requestsToSendAsArray);
                PutReceivedResponsesInTempResponseArray(tempResponseArray, receivedResponses);
            }

            AfterSendingRequests(requestsToProcess);
            BeforeReturningResponses(tempResponseArray);
            return tempResponseArray;
        }

        private void GetCachedResponsesAndRemoveThoseRequests(Request[] requestsToProcess, Response[] tempResponseArray, List<Request> requestsToSend)
        {
            for (int i = 0; i < requestsToProcess.Length; i++)
            {
                var request = requestsToProcess[i];

                if (_cacheManager.IsCachingEnabledFor(request.GetType()))
                {
                    var cachedResponse = _cacheManager.GetCachedResponseFor(request);

                    if (cachedResponse != null)
                    {
                        tempResponseArray[i] = cachedResponse;
                        requestsToSend.Remove(request);
                    }
                }
            }
        }

        private void AddCacheableResponsesToCache(Response[] receivedResponses, Request[] requestsToSend)
        {
            for (int i = 0; i < receivedResponses.Length; i++)
            {
                if (receivedResponses[i].ExceptionType == ExceptionType.None && _cacheManager.IsCachingEnabledFor(requestsToSend[i].GetType()))
                {
                    _cacheManager.StoreInCache(requestsToSend[i], receivedResponses[i]);
                }
            }
        }

        private void PutReceivedResponsesInTempResponseArray(Response[] tempResponseArray, Response[] receivedResponses)
        {
            int takeIndex = 0;

            for (int i = 0; i < tempResponseArray.Length; i++)
            {
                if (tempResponseArray[i] == null)
                {
                    tempResponseArray[i] = receivedResponses[takeIndex++];
                }
            }
        }

        protected virtual void BeforeSendingRequests(IEnumerable<Request> requestsToProcess) {}
        protected virtual void AfterSendingRequests(IEnumerable<Request> sentRequests) {}
        protected virtual void BeforeReturningResponses(IEnumerable<Response> receivedResponses) {}

        private async Task SendRequestsIfNecessary()
        {
            if (!RequestsSent()) {
                _responses = await GetResponses(_requests.ToArray());
                DealWithPossibleExceptions(_responses);
            }
        }

        private bool RequestsSent()
        {
            return _responses != null;
        }

        private void DealWithPossibleExceptions(IEnumerable<Response> responsesToCheck)
        {
            foreach (var response in responsesToCheck)
            {
                if (response.ExceptionType == ExceptionType.Security)
                {
                    DealWithSecurityException(response.Exception);
                }

                if (response.ExceptionType == ExceptionType.Unknown)
                {
                    DealWithUnknownException(response.Exception);
                }
            }
        }

        protected virtual void DealWithUnknownException(ExceptionInfo exception) { }

        protected virtual void DealWithSecurityException(ExceptionInfo exceptionDetail) { }

        private void AddRequest(Request request, bool wasAddedWithKey)
        {
            if (RequestsSent()) {
                throw new InvalidOperationException("Requests where already send. Either add request earlier or call Clear.");
            }

            var requestType = request.GetType();

            if (RequestTypeIsAlreadyPresent(requestType) &&
                (RequestTypeIsNotAssociatedWithKey(requestType) || !wasAddedWithKey))
            {
                throw new InvalidOperationException(
                    $"A request of type {requestType.FullName} has already been added. " +
                    "Please add requests of the same type with a different key.");
            }

            _requests.Add(request);
        }

        private bool RequestTypeIsNotAssociatedWithKey(Type requestType)
        {
            return !_keyToTypes.Values.Contains(requestType);
        }

        private bool RequestTypeIsAlreadyPresent(Type requestType)
        {
            return _requests.Any(r => r.GetType() == requestType);
        }
    }
}