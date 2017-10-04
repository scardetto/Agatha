using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Agatha.Common;
using Agatha.Common.InversionOfControl;
using Agatha.ServiceLayer.Logging;

namespace Agatha.ServiceLayer
{
    public class PerformanceLoggingRequestProcessor : RequestProcessor
    {
        public PerformanceLoggingRequestProcessor(IContainer container, ServiceLayerConfiguration serviceLayerConfiguration, IRequestProcessingErrorHandler errorHandler)
            : base(container, serviceLayerConfiguration, errorHandler) {}

        private readonly ILog _performanceLogger = LogProvider.GetLogger("AgathaPerformance");

        private Stopwatch _requestStopwatch;
        private Stopwatch _batchStopwatch;

        protected override void BeforeProcessing(IEnumerable<Request> requests)
        {
            base.BeforeProcessing(requests);
            _batchStopwatch = Stopwatch.StartNew();
        }

        protected override void AfterProcessing(IEnumerable<Request> requests, IEnumerable<Response> responses)
        {
            base.AfterProcessing(requests, responses);
            _batchStopwatch.Stop();

            // TODO: make the 200ms limit configurable
            if (_batchStopwatch.ElapsedMilliseconds > 200) {
                var builder = new StringBuilder();

                foreach (var request in requests) {
                    builder.Append(request.GetType().Name + ", ");
                }

                builder.Remove(builder.Length - 2, 2);

                _performanceLogger.Warn($"Performance warning: {_batchStopwatch.ElapsedMilliseconds}ms for the following batch: {builder}");
            }
        }

        protected override void BeforeHandle(Request request)
        {
            base.BeforeHandle(request);
            _requestStopwatch = Stopwatch.StartNew();
        }

        protected override void AfterHandle(Request request)
        {
            base.AfterHandle(request);
            _requestStopwatch.Stop();

            // TODO: make the 100ms limit configurable
            if (_requestStopwatch.ElapsedMilliseconds > 100)             {
                _performanceLogger.Warn($"Performance warning: {_requestStopwatch.ElapsedMilliseconds}ms for {request.GetType().Name}");
            }
        }
    }
}