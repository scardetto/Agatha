using System.ServiceModel;
using System.Threading.Tasks;

namespace Agatha.Common.WCF
{
	[ServiceContract]
	public interface IWcfRequestProcessor
	{
		[OperationContract(Name = "ProcessRequests")]
		[ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
		[TransactionFlow(TransactionFlowOption.Allowed)]
		Response[] Process(params Request[] requests);

		[OperationContract(Name = "ProcessRequestsAsync")]
		[ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
		[TransactionFlow(TransactionFlowOption.Allowed)]
		Task<Response[]> ProcessAsync(params Request[] requests);
	}
}