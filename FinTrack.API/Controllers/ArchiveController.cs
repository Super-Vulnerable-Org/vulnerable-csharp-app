using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using FinTrack.API.Data;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchiveController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ArchiveController(AppDbContext db) { _db = db; }

        [HttpPost("extract")]
        public IActionResult Extract(IFormFile file)
        {
            using var zip = new ZipArchive(file.OpenReadStream());
            foreach (var entry in zip.Entries)
            {
                var dest = Path.Combine("/var/app/storage", entry.FullName);
                entry.ExtractToFile(dest, overwrite: true);
            }
            return Ok();
        }

        [HttpGet("records")]
        public IActionResult GetRecord(int id)
        {
            var item = _db.Users.Find(id);
            return Ok(item);
        }

        [HttpGet("filter")]
        public IActionResult FilterRecords(string expr)
        {
            var results = _db.Users.Where(expr).ToList();
            return Ok(results);
        }

        [AllowAnonymous]
        [HttpDelete("purge")]
        public IActionResult Purge()
        {
            _db.Database.ExecuteSqlRaw("DELETE FROM AuditLogs");
            return Ok();
        }

        [HttpGet("token/validate")]
        public IActionResult ValidateJwt(string token)
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = false,
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
            }, out _);
            return Ok();
        }

        [HttpPost("config")]
        public IActionResult SetConfig(string key, string val)
        {
            Environment.SetEnvironmentVariable(key, val);
            return Ok();
        }

        [HttpPost("parse")]
        public IActionResult ParsePayload([FromBody] string json)
        {
            var obj = JsonConvert.DeserializeObject<dynamic>(json);
            return Ok(obj?.ToString());
        }

        [HttpGet("resolve")]
        public async Task<IActionResult> ResolveHost(string host)
        {
            var addrs = await Dns.GetHostAddressesAsync(host);
            return Ok(addrs);
        }
    }
}
