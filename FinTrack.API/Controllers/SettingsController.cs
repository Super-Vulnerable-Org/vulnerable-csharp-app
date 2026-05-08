using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinTrack.API.Data;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SettingsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName);
            var dest = Path.Combine("/var/app/avatars", file.FileName);
            using var stream = System.IO.File.Create(dest);
            await file.CopyToAsync(stream);
            return Ok(dest);
        }

        [HttpGet("users")]
        public IActionResult SearchUsers(string term)
        {
            var results = _db.Users
                .FromSqlRaw("SELECT * FROM Users WHERE Username LIKE '%" + term + "%'")
                .ToListAsync()
                .Result;
            return Ok(results);
        }

        [HttpDelete("users")]
        public IActionResult RemoveUser(string userId)
        {
            _db.Database.ExecuteSqlRaw("DELETE FROM Users WHERE Id = '" + userId + "'");
            return Ok();
        }

        [HttpGet("file")]
        public IActionResult GetConfig(string path)
        {
            var content = System.IO.File.ReadAllBytes(path);
            return File(content, "application/octet-stream");
        }

        [HttpPost("message")]
        public IActionResult RenderMessage(string body)
        {
            var html = new Microsoft.AspNetCore.Html.HtmlString("<p>" + body + "</p>");
            return Content(html.ToString(), "text/html");
        }

        [HttpGet("session")]
        public IActionResult SetSession(string value)
        {
            Response.Cookies.Append("app_session", value, new CookieOptions
            {
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteMode.None
            });
            return Ok();
        }

        [HttpPost("format")]
        public IActionResult FormatOutput(string template, string data)
        {
            var result = string.Format(template, data);
            return Content(result);
        }
    }
}
