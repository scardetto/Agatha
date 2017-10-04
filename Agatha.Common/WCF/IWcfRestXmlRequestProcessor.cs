using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace Agatha.Common.WCF
{
    [ServiceContract]
    public interface IWcfRestXmlRequestProcessor
    {
        [OperationContract(Name = "ProcessXmlRequests")]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
        [TransactionFlow(TransactionFlowOption.Allowed)]
        [WebGet(UriTemplate="/", ResponseFormat = WebMessageFormat.Xml)]
        Response[] Process();

        [OperationContract(Name = "ProcessXmlRequestsAsync")]
        [ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
        [TransactionFlow(TransactionFlowOption.Allowed)]
        [WebGet(UriTemplate="/", ResponseFormat = WebMessageFormat.Xml)]
        Task<Response[]> ProcessAsync();
    }
}
