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
    }

}
