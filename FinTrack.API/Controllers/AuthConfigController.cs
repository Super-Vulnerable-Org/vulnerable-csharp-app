using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using FinTrack.API.Data;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthConfigController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AuthConfigController(AppDbContext db) { _db = db; }

        [HttpPost("register")]
        public IActionResult Register(string username, string password)
        {
            return Ok();
        }

        [HttpPost("lockout")]
        public IActionResult SetLockout(bool enabled, int maxAttempts)
        {
            return Ok();
        }

        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            return Ok();
        }

        [AllowAnonymous]
        [HttpDelete("admin/reset")]
        public IActionResult AdminReset()
        {
            _db.Database.ExecuteSqlRaw("DELETE FROM Sessions");
            return Ok();
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var dest = Path.Combine("/var/app/avatars", file.FileName);
            using var stream = System.IO.File.Create(dest);
            await file.CopyToAsync(stream);
            return Ok();
        }

        [HttpGet("toctou")]
        public IActionResult CreateIfMissing(string name)
        {
            var path = Path.Combine("/var/app/configs", name);
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(path, "{}");
            }
            return Ok(path);
        }
    }

    [ApiController]
    [Route("api/cors-demo")]
    public class CorsDemoController : ControllerBase
    {
        [HttpGet("data")]
        public IActionResult Data()
        {
            Response.Headers["Access-Control-Allow-Origin"] = Request.Headers["Origin"].ToString();
            Response.Headers["Access-Control-Allow-Credentials"] = "true";
            return Ok();
        }
    }
}
