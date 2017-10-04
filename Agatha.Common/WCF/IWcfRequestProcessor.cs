using System.ServiceModel;

namespace Agatha.Common.WCF
{
	[ServiceContract]
	public interface IWcfRequestProcessor
	{
		[OperationContract(Name = "ProcessRequests")]
		[ServiceKnownType("GetKnownTypes", typeof(KnownTypeProvider))]
		[TransactionFlow(TransactionFlowOption.Allowed)]
		Response[] Process(params Request[] requests);
	}
}