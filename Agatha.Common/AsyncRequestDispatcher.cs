using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Agatha.Common.Caching;

namespace Agatha.Common
{
    /// <summary>
    /// Do not use this type directly, use it via an IAsyncRequestDispatcherFactory
    /// </summary>
    public interface IAsyncRequestDispatcher : IDisposable
    {
        void Add(Request request);
        void Add<TRequest>(Action<TRequest> action) where TRequest : Request, new();
        void Add(params Request[] requestsToAdd);
        void Add(string key, Request request);
        void Add(params OneWayRequest[] oneWayRequests);
        void ProcessOneWayRequests();
        void ProcessRequests(Action<ReceivedResponses> receivedResponsesDelegate, Action<ExceptionInfo> exceptionOccurredDelegate);
        void ProcessRequests(Action<ReceivedResponses> receivedResponsesDelegate, Action<ExceptionInfo, ExceptionType> exceptionAndTypeOccurredDelegate);
    }

    // TODO: make sure that OneWayRequests can't be added through the Add methods

    public class AsyncRequestDispatcher : Disposable, IAsyncRequestDispatcher
    {
        private readonly IAsyncRequestProcessor _requestProcessor;
        private readonly ICacheManager _cacheManager;
        protected Dictionary<string, int> KeyToResultPositions;
        private Dictionary<string, Type> _keyToTypes;

        private bool _oneWayRequestsAdded;
        private bool _twoWayRequestsAdded;

        private List<Request> _queuedRequests;
        private List<OneWayRequest> _queuedOneWayRequests;

        public AsyncRequestDispatcher(IAsyncRequestProcessor requestProcessor, ICacheManager cacheManager)
        {
            _requestProcessor = requestProcessor;
            _cacheManager = cacheManager;
            InitializeState();
        }

        public virtual Request[] QueuedRequests => _queuedRequests.ToArray();

        public virtual void Add(params Request[] requestsToAdd)
        {
            foreach (var request in requestsToAdd)
            {
                Add(request);
            }
        }

        public virtual void Add(string key, Request request)
        {
            AddRequest(request, true);
            _keyToTypes[key] = request.GetType();
            KeyToResultPositions[key] = _queuedRequests.Count - 1;
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

        public virtual void Add(params OneWayRequest[] oneWayRequests)
        {
            EnsureWeOnlyHaveOneWayRequests();
            _queuedOneWayRequests.AddRange(oneWayRequests);
        }

        public virtual void ProcessOneWayRequests()
        {
            var requests = _queuedOneWayRequests.ToArray();
            BeforeSendingRequests(requests);
            _requestProcessor.ProcessOneWayRequestsAsync(requests, OnProcessOneWayRequestsCompleted);
            AfterSendingRequests(requests);
            _queuedOneWayRequests.Clear();
        }

        private void OnProcessOneWayRequestsCompleted(AsyncCompletedEventArgs args)
        {
            Dispose();

            if (args.Error != null)
            {
                throw new InvalidOperationException("Exception occurred during processing of one-way requests", args.Error);
            }
        }

        public virtual void ProcessRequests(Action<ReceivedResponses> receivedResponsesDelegate, Action<ExceptionInfo> exceptionOccurredDelegate)
        {
            ProcessRequests(new ResponseReceiver(receivedResponsesDelegate, exceptionOccurredDelegate, KeyToResultPositions, _cacheManager));
        }

        public virtual void ProcessRequests(Action<ReceivedResponses> receivedResponsesDelegate, Action<ExceptionInfo, ExceptionType> exceptionAndTypeOccurredDelegate)
        {
            ProcessRequests(new ResponseReceiver(receivedResponsesDelegate, exceptionAndTypeOccurredDelegate, KeyToResultPositions, _cacheManager));
        }

        private void ProcessRequests(ResponseReceiver responseReciever)
        {
            var requestsToProcess = _queuedRequests.ToArray();

            BeforeSendingRequests(requestsToProcess);

            var tempResponseArray = new Response[requestsToProcess.Length];
            var requestsToSend = new List<Request>(requestsToProcess);

            GetCachedResponsesAndRemoveThoseRequests(requestsToProcess, tempResponseArray, requestsToSend);
            var requestsToSendAsArray = requestsToSend.ToArray();

            if (requestsToSendAsArray.Length > 0)
            {
                _requestProcessor.ProcessRequestsAsync(requestsToSendAsArray,
                    a => OnProcessRequestsCompleted(a, responseReciever, tempResponseArray, requestsToSendAsArray));
            }
            else
            {
                var synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
                synchronizationContext.Post(s => OnProcessRequestsCompleted(null, responseReciever, tempResponseArray, requestsToSendAsArray), null);
            }

            AfterSendingRequests(requestsToProcess);
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

        protected virtual void BeforeSendingRequests(IEnumerable<Request> requestsToProcess) {}
        protected virtual void AfterSendingRequests(IEnumerable<Request> sentRequests) {}

        public virtual void OnProcessRequestsCompleted(ProcessRequestsAsyncCompletedArgs args, ResponseReceiver responseReciever,
            Response[] tempResponseArray, Request[] requestsToSendAsArray)
        {
            Dispose();
            responseReciever.ReceiveResponses(args, tempResponseArray, requestsToSendAsArray);
        }

        protected override void DisposeManagedResources()
        {
            _requestProcessor?.Dispose();
        }

        private void AddRequest(Request request, bool wasAddedWithKey)
        {
            EnsureWeOnlyHaveTwoWayRequests();

            Type requestType = request.GetType();

            if (RequestTypeIsAlreadyPresent(requestType) &&
                (RequestTypeIsNotAssociatedWithKey(requestType) || !wasAddedWithKey))
            {
                throw new InvalidOperationException(
                    $"A request of type {requestType.FullName} has already been added. " +
                    "Please add requests of the same type with a different key.");
            }

            _queuedRequests.Add(request);
        }

        private bool RequestTypeIsAlreadyPresent(Type requestType)
        {
            return QueuedRequests.Any(r => r.GetType().Equals(requestType));
        }

        private bool RequestTypeIsNotAssociatedWithKey(Type requestType)
        {
            return !_keyToTypes.Values.Contains(requestType);
        }

        private void InitializeState()
        {
            _queuedRequests = new List<Request>();
            _queuedOneWayRequests = new List<OneWayRequest>();
            _keyToTypes = new Dictionary<string, Type>();
            KeyToResultPositions = new Dictionary<string, int>();
        }

        private void EnsureWeOnlyHaveOneWayRequests()
        {
            if (_twoWayRequestsAdded)
            {
                ThrowInvalidUsageException();
            }

            _oneWayRequestsAdded = true;
        }

        private void EnsureWeOnlyHaveTwoWayRequests()
        {
            if (_oneWayRequestsAdded)
            {
                ThrowInvalidUsageException();
            }

            _twoWayRequestsAdded = true;
        }

        private void ThrowInvalidUsageException()
        {
            throw new InvalidOperationException("You cannot combine one-way and two-way requests in the same AsyncRequestDispatcher.");
        }
    }
}