using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HeadersController : ControllerBase
    {
        [HttpGet("inject")]
        public IActionResult InjectHeader(string value)
        {
            Response.Headers["X-Custom-Header"] = value;
            Response.Headers.Add("X-Forward", value);
            return Ok();
        }

        [HttpGet("page")]
        public IActionResult Page()
        {
            return Content("<html><body>page</body></html>", "text/html");
        }
    }

    [ApiController]
    [Route("api/listener")]
    public class ListenerController : ControllerBase
    {
        [HttpGet("start")]
        public IActionResult StartListener()
        {
            var listener = new System.Net.HttpListener();
            listener.Prefixes.Add("http://*:9090/");
            listener.Start();
            return Ok();
        }
    }
}
