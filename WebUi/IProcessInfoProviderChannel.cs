using System.ServiceModel;
using Core.Contracts;

namespace WebUi
{
    public interface IProcessInfoProviderChannel : IProcessInfoProvider, IClientChannel
    {
    }
}