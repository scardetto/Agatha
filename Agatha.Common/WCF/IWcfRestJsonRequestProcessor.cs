using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace Agatha.Common.WCF
{
    [ServiceContract]
    public interface IWcfRestJsonRequestProcessor
    {
        [OperationContract(Name = "ProcessJsonRequestsGet")]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
        [TransactionFlow(TransactionFlowOption.Allowed)]
        [WebGet(UriTemplate = "/", BodyStyle = WebMessageBodyStyle.WrappedResponse, ResponseFormat = WebMessageFormat.Json)]
        Response[] Process();

        [OperationContract(Name = "ProcessJsonRequestsPost")]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
        [TransactionFlow(TransactionFlowOption.Allowed)]
        [WebInvoke(UriTemplate = "/post", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        Response[] Process(Request[] requests);

        [OperationContract(Name = "ProcessJsonRequestsGetAsync")]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
        [TransactionFlow(TransactionFlowOption.Allowed)]
        [WebGet(UriTemplate = "/", BodyStyle = WebMessageBodyStyle.WrappedResponse, ResponseFormat = WebMessageFormat.Json)]
        Task<Response[]> ProcessAsync();

        [OperationContract(Name = "ProcessJsonRequestsPostAsync")]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
        [TransactionFlow(TransactionFlowOption.Allowed)]
        [WebInvoke(UriTemplate = "/post", BodyStyle = WebMessageBodyStyle.Wrapped, ResponseFormat = WebMessageFormat.Json)]
        Task<Response[]> ProcessAsync(Request[] requests);

    }
}
