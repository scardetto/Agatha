﻿using System;
using Agatha.Common;
using Agatha.Common.Caching;
using Agatha.Common.InversionOfControl;

namespace Agatha.ServiceLayer
{
    public interface IRequestProcessingErrorHandler
    {
        void DealWithException(RequestProcessingContext context, Exception exception);

        void DealWithPreviouslyOccurredExceptions(RequestProcessingContext context);
    }

    public class RequestProcessingErrorHandler : IRequestProcessingErrorHandler
    {
        private readonly IContainer _container;
        private readonly ServiceLayerConfiguration _serviceLayerConfiguration;

        public RequestProcessingErrorHandler(IContainer container, ServiceLayerConfiguration serviceLayerConfiguration)
        {
            _container = container;
            _serviceLayerConfiguration = serviceLayerConfiguration;
        }

        public void DealWithException(RequestProcessingContext context, Exception exception)
        {
            var response = CreateResponse(context);
            response.Exception = new ExceptionInfo(exception);
            SetExceptionType(response, exception);
            context.MarkAsProcessed(response);
        }

        public void DealWithPreviouslyOccurredExceptions(RequestProcessingContext context)
        {
            var response = CreateResponse(context);
            response.Exception = new ExceptionInfo(new Exception(ExceptionType.EarlierRequestAlreadyFailed.ToString()));
            response.ExceptionType = ExceptionType.EarlierRequestAlreadyFailed;
            context.MarkAsProcessed(response);
        }

        private Response CreateResponse(RequestProcessingContext context)
        {
            var responseType = DetermineResponseType(context);
            return (Response)Activator.CreateInstance(responseType);
        }

        private Type DetermineResponseType(RequestProcessingContext context)
        {
            var strategies = new Func<RequestProcessingContext, Type>[]
                {
                        TryBasedOnConventions,
                        TryBasedOnCachedResponse,
                        TryBasedOnRequestHandler
                };
            foreach (var strategy in strategies)
            {
                var responseType = strategy(context);
                if (responseType != null) return responseType;
            }
            return typeof(Response);
        }

        private Type TryBasedOnConventions(RequestProcessingContext context)
        {
            var conventions = _container.TryResolve<IConventions>();

            return conventions?.GetResponseTypeFor(context.Request);
        }

        private Type TryBasedOnCachedResponse(RequestProcessingContext context)
        {
            var cacheManager = _container.Resolve<ICacheManager>();
            if (cacheManager.IsCachingEnabledFor(context.Request.GetType()))
            {
                var response = cacheManager.GetCachedResponseFor(context.Request);
                if (response != null) return response.GetType();
            }
            return null;
        }

        private Type TryBasedOnRequestHandler(RequestProcessingContext context)
        {
            try {
                var handler = (IRequestHandler)_container.Resolve(GetRequestHandlerTypeFor(context.Request));
                return handler.CreateDefaultResponse().GetType();
            } catch {
                return null;
            }
        }

        private static Type GetRequestHandlerTypeFor(Request request)
        {
            return typeof(IRequestHandler<>).MakeGenericType(request.GetType());
        }

        private void SetExceptionType(Response response, Exception exception)
        {
            var exceptionType = exception.GetType();

            if (exceptionType == _serviceLayerConfiguration.BusinessExceptionType)
            {
                response.ExceptionType = ExceptionType.Business;
                SetExceptionFaultCode(exception, response.Exception);

                return;
            }

            if (exceptionType == _serviceLayerConfiguration.SecurityExceptionType)
            {
                response.ExceptionType = ExceptionType.Security;
                SetExceptionFaultCode(exception, response.Exception);
                return;
            }

            response.ExceptionType = ExceptionType.Unknown;
        }

        private void SetExceptionFaultCode(Exception exception, ExceptionInfo exceptionInfo)
        {
            var businessExceptionType = exception.GetType();

            var faultCodeProperty = businessExceptionType.GetProperty("FaultCode");

            if (faultCodeProperty != null
                && faultCodeProperty.CanRead
                && faultCodeProperty.PropertyType == typeof(string))
            {
                exceptionInfo.FaultCode = (string)faultCodeProperty.GetValue(exception, null);
            }
        }
    }
}