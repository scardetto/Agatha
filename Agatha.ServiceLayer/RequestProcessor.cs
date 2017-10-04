using System;
using System.Collections.Generic;
using System.Linq;
using Agatha.Common;
using Agatha.Common.Interceptors;
using Agatha.Common.InversionOfControl;
using Agatha.ServiceLayer.Logging;

namespace Agatha.ServiceLayer
{
    public class RequestProcessor : Disposable, IRequestProcessor
    {
        private readonly ServiceLayerConfiguration _serviceLayerConfiguration;
        private readonly IContainer _container;
        private readonly ILog _logger = LogProvider.GetLogger(typeof(RequestProcessor));
        private readonly IRequestProcessingErrorHandler _errorHandler;

        protected override void DisposeManagedResources()
        {
            // empty by default but you should override this in derived classes so you can clean up your resources
        }

        public RequestProcessor(IContainer container, ServiceLayerConfiguration serviceLayerConfiguration, IRequestProcessingErrorHandler errorHandler)
        {
            _container = container;
            _serviceLayerConfiguration = serviceLayerConfiguration;
            _errorHandler = errorHandler;
        }

        protected virtual void BeforeProcessing(IEnumerable<Request> requests) { }

        protected virtual void AfterProcessing(IEnumerable<Request> requests, IEnumerable<Response> responses) { }

        protected virtual void BeforeHandle(Request request) { }

        protected virtual void AfterHandle(Request request) { }

        public Response[] Process(params Request[] requests)
        {
            if (requests == null) return null;

            var exceptionsPreviouslyOccurred = false;

            var unitOfWork = _container.Resolve<IAgathaUnitOfWork>();
            Exception initialException = null;

            var processingContexts = requests.Select(request => new RequestProcessingContext(request)).ToList();

            foreach (var requestProcessingState in processingContexts) {
                if (exceptionsPreviouslyOccurred) {
                    _errorHandler.DealWithPreviouslyOccurredExceptions(requestProcessingState);
                    continue;
                }

                var invokedInterceptors = new List<IRequestHandlerInterceptor>();

                try {
                    var interceptors = ResolveInterceptors();

                    foreach (var interceptor in interceptors) {
                        interceptor.BeforeHandlingRequest(requestProcessingState);
                        invokedInterceptors.Add(interceptor);

                        if (requestProcessingState.IsProcessed) break;
                    }

                    if (!requestProcessingState.IsProcessed) {
                        InvokeRequestHandler(requestProcessingState);
                    }
                } catch (Exception exc) {
                    _logger.ErrorException(exc.Message, exc);
                    exceptionsPreviouslyOccurred = true;
                    initialException = exc;
                    _errorHandler.DealWithException(requestProcessingState, exc);
                } finally {
                    var possibleExceptionsFromInterceptors = RunInvokedInterceptorsSafely(requestProcessingState, invokedInterceptors);

                    if (possibleExceptionsFromInterceptors.Any()) {
                        foreach (var exceptionFromInterceptor in possibleExceptionsFromInterceptors) {
                            _logger.ErrorException("An unexpected error occurred in interceptor", exceptionFromInterceptor);
                        }

                        exceptionsPreviouslyOccurred = true;
                        _errorHandler.DealWithException(requestProcessingState, possibleExceptionsFromInterceptors.ElementAt(0));
                    }
                }
            }

            var responses = processingContexts.Select(c => c.Response).ToArray();
            unitOfWork.End(initialException);

            return responses;
        }

        private void InvokeRequestHandler(RequestProcessingContext requestProcessingState)
        {
            HandleRequest(requestProcessingState);
        }

        private void HandleRequest(RequestProcessingContext requestProcessingState)
        {
            var request = requestProcessingState.Request;
            var handler = (IRequestHandler) _container.Resolve(GetRequestHandlerTypeFor(request));
            var response = GetResponseFromHandler(request, handler);
            requestProcessingState.MarkAsProcessed(response);
        }

        private IList<Exception> RunInvokedInterceptorsSafely(RequestProcessingContext requestProcessingState, IList<IRequestHandlerInterceptor> invokedInterceptors)
        {
            var exceptionsFromInterceptor = new List<Exception>();

            foreach (var interceptor in invokedInterceptors.Reverse()) {
                try {
                    interceptor.AfterHandlingRequest(requestProcessingState);
                } catch (Exception exc) {
                    exceptionsFromInterceptor.Add(exc);
                }
            }

            return exceptionsFromInterceptor;
        }

        private IList<IRequestHandlerInterceptor> ResolveInterceptors()
        {
            return _serviceLayerConfiguration.GetRegisteredInterceptorTypes()
                .Select(t => (IRequestHandlerInterceptor)_container.Resolve(t)).ToList();
        }

        private static Type GetRequestHandlerTypeFor(Request request)
        {
            // get a type reference to IRequestHandler<ThisSpecificRequestType>
            return typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        }

        private Response GetResponseFromHandler(Request request, IRequestHandler handler)
        {
            try {
                var response = handler.Handle(request);
                return response;
            } catch (Exception e) {
                OnHandlerException(request, e);
                throw;
            }
        }

        protected virtual void OnHandlerException(Request request, Exception exception) { }
    }
}