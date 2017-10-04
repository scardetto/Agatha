using System.ServiceModel;
using System.ServiceModel.Activation;
using Agatha.Common;
using Agatha.Common.InversionOfControl;
using Agatha.Common.WCF;
using System.ServiceModel.Web;
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
            using (var container = GetContainer()) {
                var processor = container.Resolve<IRequestProcessor>();
                return processor.Process(requests);
            }
        }

        public Response[] Process()
        {
            var collection = WebOperationContext.Current?.IncomingRequest.UriTemplateMatch.QueryParameters;

            var builder = new RestRequestBuilder();

            var requests = builder.GetRequests(collection);

            return Process(requests);
        }

        private IContainer GetContainer()
        {
            var container = IoC.Container.GetChildContainer();
            container.RegisterInstance(typeof(IContainer), container);

            return container;
        }
    }
}