using System;
using System.Threading.Tasks;
using Agatha.Common;

namespace Agatha.ServiceLayer
{
    public interface IRequestHandler : IDisposable
    {
        Task<Response> Handle(Request request);
        Response CreateDefaultResponse();
    }

    public interface IRequestHandler<in TRequest> : IRequestHandler where TRequest : Request
    {
        Task<Response> Handle(TRequest request);
    }

    public abstract class RequestHandler : Disposable, IRequestHandler
    {
        public abstract Task<Response> Handle(Request request);
        public abstract Response CreateDefaultResponse();

        /// <summary>
        /// Default implementation is empty
        /// </summary>
        protected override void DisposeManagedResources() { }
    }

    public abstract class RequestHandler<TRequest, TResponse> : RequestHandler, IRequestHandler<TRequest>, ITypedRequestHandler
        where TRequest : Request
        where TResponse : Response, new()
    {
        public override Task<Response> Handle(Request request)
        {
            var typedRequest = (TRequest)request;
            return Handle(typedRequest);
        }

        public abstract Task<Response> Handle(TRequest request);

        public override Response CreateDefaultResponse()
        {
            return CreateTypedResponse();
        }

        public TResponse CreateTypedResponse()
        {
            return new TResponse();
        }
    }

    /// <summary>
    /// This is just a marker interface to indicate that the request handler specifies a response type
    /// </summary>
    public interface ITypedRequestHandler
    {
    }
}