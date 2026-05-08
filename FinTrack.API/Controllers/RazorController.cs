using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RazorController : ControllerBase
    {
        [HttpGet("raw")]
        public IActionResult RawHtml(string content)
        {
            var html = new HtmlString("<div>" + content + "</div>");
            return Content(html.ToString(), "text/html");
        }

        [HttpGet("dynamic")]
        public IActionResult DynamicCode(string template)
        {
            var result = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(template);
            return Ok("compiled");
        }
    }

    [ApiController]
    [Route("api/xpath")]
    public class XPathController : ControllerBase
    {
        [HttpGet("query")]
        public IActionResult XPathQuery(string query)
        {
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml("<root><item>value</item></root>");
            var nav = doc.CreateNavigator();
            var result = nav?.SelectSingleNode(query);
            return Ok(result?.Value);
        }
    }

    [ApiController]
    [Route("api/directory")]
    public class DirectoryListingController : ControllerBase
    {
        [HttpGet("list")]
        public IActionResult ListDir(string path)
        {
            var files = System.IO.Directory.GetFiles(path);
            return Ok(files);
        }
    }
}
