using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace RouteGuardian.Test.Mocks;

public class HttpContextAccessorMock : Mock<IHttpContextAccessor> 
{
    public HttpContextAccessorMock(string mockContext, ClaimsPrincipal testUser, string? token = null)
    {
        switch (mockContext)
        {
            case "ctx1":
            case "ctx2":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser
                    });
                break;
            case "ctx3":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Request =
                        {
                            Method = "GET",
                            Path = "/api/test/test",
                            Headers =
                            {
                                new (Const.AuthHeader, $"{Const.BearerTokenPrefix}{token}")
                            }
                        }                        
                    });
                break;
            case "ctx4":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Request =
                        {
                            Method = "GET",
                            Path = "/api/foo",
                            Headers =
                            {
                                new (Const.AuthHeader, $"{Const.BearerTokenPrefix}{token}")
                            }
                        }                        
                    });
                break;            
        }
        
    }
}