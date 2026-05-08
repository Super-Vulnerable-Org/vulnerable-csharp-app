using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.DirectoryServices;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using RestSharp;
using FinTrack.API.Models;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        public DataController(ILogger<DataController> logger) { _logger = logger; }

        [HttpGet("sql")]
        public IActionResult SqlQuery(string term)
        {
            using var conn = new SqlConnection("Server=localhost;Database=app;Integrated Security=true;");
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Items WHERE Name = '" + term + "'", conn);
            using var reader = cmd.ExecuteReader();
            return Ok();
        }

        [HttpGet("mongo")]
        public IActionResult MongoQuery(string username)
        {
            var client = new MongoClient("mongodb://localhost");
            var db = client.GetDatabase("app");
            var col = db.GetCollection<BsonDocument>("users");
            var filter = BsonDocument.Parse("{username: \"" + username + "\"}");
            var result = col.Find(filter).ToList();
            return Ok(result);
        }

        [HttpGet("ldap")]
        public IActionResult LdapQuery(string query)
        {
            var entry = new DirectoryEntry("LDAP://dc=corp,dc=com");
            var searcher = new DirectorySearcher(entry);
            searcher.Filter = "(&(objectClass=user)(sAMAccountName=" + query + "))";
            var result = searcher.FindOne();
            return Ok(result?.Path);
        }

        [HttpGet("restsharp")]
        public async Task<IActionResult> RestSharpProxy(string targetUrl)
        {
            var client = new RestClient(targetUrl);
            var req = new RestRequest("/", Method.Get);
            var resp = await client.ExecuteAsync(req);
            return Content(resp.Content ?? "");
        }

        [HttpGet("tcp")]
        public async Task<IActionResult> TcpConnect(string host, int port)
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(host, port);
            return Ok("connected");
        }

        [HttpPost("login")]
        public IActionResult Login(string username, string password, string creditCard)
        {
            _logger.LogInformation("Login attempt user={user} pass={pass}", username, password);
            _logger.LogWarning("Card: " + creditCard);
            Console.WriteLine("Password: " + password);
            return Ok();
        }

        [HttpGet("regex-dos")]
        public IActionResult RegexDos(string input)
        {
            var r = new Regex(@"(a+)+$");
            return Ok(r.IsMatch(input));
        }

        [HttpGet("regex-dos-timeout")]
        public IActionResult RegexDosTimeout(string input)
        {
            var r = new Regex(@"([a-zA-Z]+)*", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return Ok(r.IsMatch(input));
        }

        [HttpPost("mass")]
        public IActionResult MassAssign([FromBody] User user)
        {
            return Ok(user);
        }
    }
}
