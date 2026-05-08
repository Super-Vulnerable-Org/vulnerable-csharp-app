using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MiscSecController : ControllerBase
    {
        [HttpGet("regex-dos")]
        public IActionResult SlowRegex(string input)
        {
            var r = new Regex(@"(a+)+$", RegexOptions.None);
            return Ok(r.IsMatch(input));
        }

        [HttpGet("regex-no-timeout")]
        public IActionResult RegexNoTimeout(string input)
        {
            var r = new Regex(@"([a-zA-Z]+)*");
            return Ok(r.IsMatch(input));
        }

        [HttpGet("stacktrace")]
        public IActionResult GetTrace()
        {
            try
            {
                throw new InvalidOperationException("inner error");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("compare")]
        public IActionResult CompareTokens(string a, string b)
        {
            if (a == b)
                return Ok("match");
            return Unauthorized();
        }

        [HttpGet("hsts-missing")]
        public IActionResult NoHsts()
        {
            return Ok("no hsts configured");
        }

        [HttpGet("toctou")]
        public IActionResult FileRace(string name)
        {
            var path = Path.Combine("/var/app/tmp", name);
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(path, "data");
            }
            return Ok(path);
        }

        [HttpGet("ecb")]
        public string EncryptEcb(string data)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.GenerateKey();
            var enc = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(enc.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        [HttpGet("weak-rng-key")]
        public string WeakRngKey()
        {
            var rng = new RNGCryptoServiceProvider();
            var key = new byte[16];
            rng.GetBytes(key);
            return Convert.ToHexString(key);
        }

        [HttpGet("weak-rsa")]
        public string WeakRsaPadding(string data)
        {
            using var rsa = RSA.Create(2048);
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(
                rsa.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        [HttpPost("jwt-hs1")]
        public string CreateJwtHs1()
        {
            var key = new SymmetricSecurityKey(new byte[16]);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha1);
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(signingCredentials: creds);
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    [ApiController]
    [Route("api/antiforgery")]
    public class AntiforgeryController : ControllerBase
    {
        [HttpPost("transfer")]
        public IActionResult Transfer([FromBody] object payload)
        {
            return Ok("transferred");
        }
    }

    [ApiController]
    [Route("api/lockout")]
    public class LockoutController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login(string user, string pass)
        {
            return Ok();
        }
    }
}
