using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace RouteGuardian.Test.Mocks;

public class HttpContextApiKeyAccessorMock : Mock<IHttpContextAccessor> 
{
    public HttpContextApiKeyAccessorMock(string mockContext, ClaimsPrincipal testUser, string? token = null)
    {
        switch (mockContext)
        {
            case "ctxNull":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser
                    });
                break;
            case "ctxEmpty":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Request =
                        {
                            Headers =
                            {
                                new (Const.HeaderClientId, ""),
                                new (Const.HeaderClientKey, ""),
                            }
                        }                        
                    });
                break;
            case "ctxInvalidClientId":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Connection =
                        {
                            RemoteIpAddress = IPAddress.Parse("0.0.0.0")
                        },
                        Request =
                        {
                            
                            Headers =
                            {
                                new (Const.HeaderClientId, "6a3ffb74-b1bc-455c-8628-e9f300d93xXx"),
                                new (Const.HeaderClientKey, "xO3O00@(W}?)k.XG\u00a39G'/uX23nM/?$RqF)nktn4<xi~9pq.\u00a3bA"),
                            }
                        }                        
                    });
                break;            
            case "ctxInvalidIp":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Connection =
                        {
                            RemoteIpAddress = IPAddress.Parse("0.8.1.5")
                        },
                        Request =
                        {
                            
                            Headers =
                            {
                                new (Const.HeaderClientId, "6a3ffb74-b1bc-455c-8628-e9f300d934e3"),
                                new (Const.HeaderClientKey, "xO3O00@(W}?)k.XG\u00a39G'/uX23nM/?$RqF)nktn4<xi~9pq.\u00a3bA"),
                            }
                        }                        
                    });
                break;
            case "ctxInvalidClientKey":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Connection =
                        {
                            RemoteIpAddress = IPAddress.Parse("0.0.0.0")
                        },
                        Request =
                        {
                            
                            Headers =
                            {
                                new (Const.HeaderClientId, "6a3ffb74-b1bc-455c-8628-e9f300d934e3"),
                                new (Const.HeaderClientKey, "0815"),
                            }
                        }                        
                    });
                break;
            case "ctxExpiredKey":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Connection =
                        {
                            RemoteIpAddress = IPAddress.Parse("127.0.0.1")
                        },
                        Request =
                        {
                            Headers =
                            {
                                new (Const.HeaderClientId, "6a3ffb74-b1bc-455c-8628-e9f300d934e3"),
                                new (Const.HeaderClientKey, "xO3O00@(W}?)k.XG\u00a39G'/uX23nM/?$RqF)nktn4<xi~9pq.\u00a3bA"),
                            }
                        }                        
                    });
                break;            
            
            case "ctxValidKeyButNoGuardAccess":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Connection =
                        {
                            RemoteIpAddress = IPAddress.Parse("127.0.0.1")
                        },
                        Request =
                        {
                            Method = "GET",
                            Path = "/api/test/test",
                            Headers =
                            {
                                new (Const.HeaderClientId, "043a8b62-5ddc-470e-a5ff-1ee2d1e303df"),
                                new (Const.HeaderClientKey, "4'AM]zD2G)Z.7/+6d'S@/0&PoAv.]Q6Q.qci|ND,oi9z029H?X"),
                            }
                        }                        
                    });
                break;
            case "ctxValidAndGuardAccess":
                Setup(sp => sp.HttpContext)
                    .Returns(new DefaultHttpContext()
                    {
                        User = testUser,
                        Connection =
                        {
                            RemoteIpAddress = IPAddress.Parse("127.0.0.1")
                        },
                        Request =
                        {
                            Method = "GET",
                            Path = "/api/test/keytest",
                            Headers =
                            {
                                new (Const.HeaderClientId, "043a8b62-5ddc-470e-a5ff-1ee2d1e303df"),
                                new (Const.HeaderClientKey, "4'AM]zD2G)Z.7/+6d'S@/0&PoAv.]Q6Q.qci|ND,oi9z029H?X"),
                            }
                        }                        
                    });
                break;                    
        }
        
    }
}