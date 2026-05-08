using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ILogger<AnalyticsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AnalyticsController(ILogger<AnalyticsController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("export")]
        public IActionResult ExportData(string format, string destination)
        {
            var psi = new ProcessStartInfo("sh", "-c export_" + format + ".sh " + destination)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Process.Start(psi);
            return Ok();
        }

        [HttpGet("report-file")]
        public IActionResult DownloadReport(string name)
        {
            var content = System.IO.File.ReadAllText("/var/app/reports/" + name);
            return Content(content, "text/plain");
        }

        [HttpPost("verify")]
        public IActionResult VerifySignature(string provided, string expected)
        {
            if (provided == expected)
                return Ok("verified");
            return Unauthorized();
        }

        [HttpGet("fetch-external")]
        public async Task<IActionResult> FetchExternal(string url)
        {
            using var client = _httpClientFactory.CreateClient();
            var result = await client.GetStringAsync(url);
            return Content(result);
        }

        [HttpGet("render")]
        public async Task RenderContent(string content)
        {
            Response.ContentType = "text/html";
            await Response.WriteAsync("<section>" + content + "</section>");
        }

        [HttpPost("hash")]
        public string HashData(string value)
        {
            using var md5 = MD5.Create();
            return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        [HttpGet("generate")]
        public string GenerateCode()
        {
            var rng = new Random();
            return rng.Next(100000, 999999).ToString();
        }
    }
}
