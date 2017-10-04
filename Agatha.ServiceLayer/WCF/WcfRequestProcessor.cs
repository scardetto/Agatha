using System.ServiceModel;
using System.ServiceModel.Activation;
using Agatha.Common;
using Agatha.Common.InversionOfControl;
using Agatha.Common.WCF;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using Agatha.ServiceLayer.WCF.Rest;

namespace Agatha.ServiceLayer.WCF
{
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    [AddMessageInspectorBehavior]
    [AddErrorLoggingBehavior]
    public class WcfRequestProcessor : IWcfRequestProcessor, IWcfRestJsonRequestProcessor, IWcfRestXmlRequestProcessor
    {
        [TransactionFlow(TransactionFlowOption.Allowed)]
        public Response[] Process(params Request[] requests)
        {
            return ProcessAsync(requests).GetAwaiter().GetResult();
        }

        [TransactionFlow(TransactionFlowOption.Allowed)]
        public Response[] Process()
        {
            return ProcessAsync().GetAwaiter().GetResult();
        }

        [TransactionFlow(TransactionFlowOption.Allowed)]
        public async Task<Response[]> ProcessAsync(params Request[] requests)
        {
            using (var container = GetContainer()) {
                var processor = container.Resolve<IRequestProcessor>();
                return await processor.ProcessAsync(requests);
            }
        }

        [TransactionFlow(TransactionFlowOption.Allowed)]
        public Task<Response[]> ProcessAsync()
        {
            var collection = WebOperationContext.Current?.IncomingRequest.UriTemplateMatch.QueryParameters;

            var builder = new RestRequestBuilder();

            var requests = builder.GetRequests(collection);

            return ProcessAsync(requests);
        }

        private IContainer GetContainer()
        {
            var container = IoC.Container.GetChildContainer();
            container.RegisterInstance(typeof(IContainer), container);

            return container;
        }
    }
}