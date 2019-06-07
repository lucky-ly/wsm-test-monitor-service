using System.Collections.Generic;
using System.ServiceModel;

namespace Core.Contracts
{
    [ServiceContract]
    public interface IProcessInfoProvider
    {
        [OperationContract]
        IEnumerable<ProcessInfoData> GetProcesses();
    }
}
