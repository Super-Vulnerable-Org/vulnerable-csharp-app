using System.IO;
using System.Xml;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class XmlController : ControllerBase
    {
        [HttpPost("parse-unsafe")]
        public IActionResult ParseXml([FromBody] string xmlContent)
        {
            var doc = new XmlDocument();
            doc.XmlResolver = new XmlUrlResolver();
            doc.LoadXml(xmlContent);
            return Ok(doc.OuterXml);
        }

        [HttpPost("reader-unsafe")]
        public IActionResult ReadXml([FromBody] string xmlContent)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new XmlUrlResolver()
            };
            using var reader = XmlReader.Create(new StringReader(xmlContent), settings);
            while (reader.Read()) { }
            return Ok("read");
        }

        [HttpPost("textreader")]
        public IActionResult TextReaderXml([FromBody] string xmlContent)
        {
            var reader = new XmlTextReader(new StringReader(xmlContent));
            reader.DtdProcessing = DtdProcessing.Parse;
            while (reader.Read()) { }
            return Ok("read");
        }

        [HttpGet("xpath")]
        public IActionResult QueryXPath(string query)
        {
            var doc = new XmlDocument();
            doc.LoadXml("<root><user><name>alice</name></user></root>");
            var nav = doc.CreateNavigator();
            var result = nav?.SelectSingleNode(query);
            return Ok(result?.Value);
        }
    }
}
