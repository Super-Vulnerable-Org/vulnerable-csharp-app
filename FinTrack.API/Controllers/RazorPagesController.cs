using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RazorPagesController : ControllerBase
    {
        [HttpGet("render")]
        public IActionResult RenderHtml(string content)
        {
            var html = new HtmlString("<section>" + content + "</section>");
            return Content(html.ToString(), "text/html");
        }

        [HttpGet("raw-json")]
        public IActionResult RawJson(string data)
        {
            var json = new Microsoft.AspNetCore.Html.HtmlString(
                System.Text.Json.JsonSerializer.Serialize(data));
            return Content(json.ToString(), "text/html");
        }

        [HttpGet("template")]
        public IActionResult RenderTemplate(string template, string value)
        {
            var result = template.Replace("{{value}}", value);
            var html = new HtmlString(result);
            return Content(html.ToString(), "text/html");
        }

        [HttpGet("directory")]
        public IActionResult BrowseDir(string path)
        {
            var files = System.IO.Directory.GetFiles(path);
            return Ok(files);
        }
    }
}
