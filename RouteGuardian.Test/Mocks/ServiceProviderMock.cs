using Microsoft.Extensions.Configuration;
using Moq;
using RouteGuardian.Helper;

namespace RouteGuardian.Test.Mocks;

public class ServiceProviderMock : Mock<IServiceProvider> 
{
    public ServiceProviderMock(IConfiguration config)
    {
        Setup(sp => sp.GetService(typeof(IJwtHelper)))
            .Returns(new JwtHelper(config));
        Setup(sp => sp.GetService(typeof(IWinHelper)))
            .Returns(new WinHelper(config));
    }
}