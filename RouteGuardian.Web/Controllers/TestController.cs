using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RouteGuardian.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("Run")]
        public ActionResult Run() 
        {
            return Ok("And I ran!");
        }

        [HttpGet("Test")]
        public ActionResult Test() 
        {
            return Ok("Tested!");
        }
        
        [HttpGet("KeyTest")]
        [Authorize(Policy = "RouteGuardianApiKey")]
        public ActionResult KeyTest()
        {
            var client = User.Claims.FirstOrDefault(c => c.Type == "ClientName");
            return Ok($"Access for API client {client?.Value} is granted.");
        }
    }

}
