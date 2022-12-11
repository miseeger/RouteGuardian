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
            var tests = new RouteGuardianTests();
            tests.Run();
            
            return Ok("Okay");
        }

        [HttpGet("Test")]
        public ActionResult Test() 
        {

            return Ok("Okay");
        }
    }

}
