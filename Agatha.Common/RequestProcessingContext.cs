using System;

namespace Agatha.Common
{
    public class RequestProcessingContext
    {
        public Request Request { get; }
        public Response Response { get; private set; }

        public RequestProcessingContext(Request request)
        {
            Request = request;
        }

        public void MarkAsProcessed(Response response)
        {
            Response = response ?? throw new ArgumentNullException("response");
            IsProcessed = true;
        }

        public bool IsProcessed { get; private set; }
    }
}