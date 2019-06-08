using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Core.Contracts;

namespace WebUi
{
    [DebuggerStepThrough]
    public class ProcessInfoProviderClient : ClientBase<IProcessInfoProvider>, IProcessInfoProvider
    {
    
        public ProcessInfoProviderClient()
        {
        }
    
        public ProcessInfoProviderClient(string endpointConfigurationName) : 
            base(endpointConfigurationName)
        {
        }
    
        public ProcessInfoProviderClient(string endpointConfigurationName, string remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
        {
        }
    
        public ProcessInfoProviderClient(string endpointConfigurationName, EndpointAddress remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
        {
        }
    
        public ProcessInfoProviderClient(Binding binding, EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
        {
        }

        public IEnumerable<ProcessInfoData> GetProcesses()
        {
            return base.Channel.GetProcesses();
        }
    }
}