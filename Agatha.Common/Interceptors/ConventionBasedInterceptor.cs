﻿using System;
using Agatha.Common.InversionOfControl;

namespace Agatha.Common.Interceptors
{
    public abstract class ConventionBasedInterceptor : Disposable, IRequestHandlerInterceptor
    {
        public abstract void BeforeHandlingRequest(RequestProcessingContext context);
        public abstract void AfterHandlingRequest(RequestProcessingContext context);

        protected IConventions Conventions { get; }

        protected ConventionBasedInterceptor(IContainer container)
        {
            Conventions = container.Resolve<IConventions>();
        }

        public Response CreateDefaultResponseFor(Request request)
        {
            var responseType = Conventions.GetResponseTypeFor(request);
            return (Response)Activator.CreateInstance(responseType);
        }
    }
}