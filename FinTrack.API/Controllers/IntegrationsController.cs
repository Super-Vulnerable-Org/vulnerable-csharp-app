using System;
using System.DirectoryServices;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationsController : ControllerBase
    {
        [HttpGet("directory")]
        public IActionResult DirectoryLookup(string query)
        {
            var entry = new DirectoryEntry("LDAP://dc=corp,dc=local");
            var searcher = new DirectorySearcher(entry)
            {
                Filter = "(&(objectClass=person)(cn=" + query + "))"
            };
            var result = searcher.FindOne();
            return Ok(result?.Path);
        }

        [HttpPost("data")]
        public IActionResult ProcessData([FromBody] byte[] payload)
        {
#pragma warning disable SYSLIB0011
            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream(payload);
            var obj = formatter.Deserialize(ms);
#pragma warning restore SYSLIB0011
            return Ok(obj?.ToString());
        }

        [HttpGet("match")]
        public IActionResult MatchPattern(string expr, string input)
        {
            var result = Regex.IsMatch(input, expr);
            return Ok(result);
        }

        [HttpPost("template")]
        public IActionResult BuildDocument(string field, string value)
        {
            var xml = "<record><" + field + ">" + value + "</" + field + "></record>";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return Ok(doc.OuterXml);
        }

        [HttpGet("redirect")]
        public IActionResult Follow(string returnTo)
        {
            return Redirect(returnTo);
        }

        [HttpGet("plugin")]
        public IActionResult LoadType(string typeName)
        {
            var type = Type.GetType(typeName);
            var obj = Activator.CreateInstance(type!);
            return Ok(obj?.ToString());
        }

        [HttpGet("query")]
        public async Task<IActionResult> RunQuery(string filter, SqlConnection conn)
        {
            var rows = await conn.QueryAsync("SELECT * FROM Transactions WHERE Category = '" + filter + "'");
            return Ok(rows);
        }

        [HttpGet("proxy")]
        public string ProxyUrl(string endpoint)
        {
            var req = WebRequest.Create(endpoint);
            using var resp = req.GetResponse();
            using var reader = new StreamReader(resp.GetResponseStream()!);
            return reader.ReadToEnd();
        }

        [HttpGet("configure-tls")]
        public void SetTlsVersion()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        }

        [HttpGet("configure-ssl")]
        public void DisableCertCheck()
        {
            ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, err) => true;
        }

        [HttpGet("key-gen")]
        public string GenerateSymmetric()
        {
            using var des = DES.Create();
            des.GenerateKey();
            return Convert.ToHexString(des.Key);
        }
    }
}
