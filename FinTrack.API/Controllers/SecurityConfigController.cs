using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.API.Controllers
{
    // stacktrace-disclosure: UseDeveloperExceptionPage outside IsDevelopment check
    public class ProductionStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseRouting();
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SecurityConfigController : ControllerBase
    {
        // use_ecb_mode: typed as SymmetricAlgorithm
        [HttpPost("encrypt")]
        public string EncryptData(string data)
        {
            SymmetricAlgorithm algo = Aes.Create();
            algo.Mode = CipherMode.ECB;
            algo.GenerateKey();
            var enc = algo.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(enc.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        // use_weak_rng_for_keygeneration
        [HttpGet("key")]
        public string GenerateKey()
        {
            var rng = new RNGCryptoServiceProvider();
            var key = new byte[8];
            rng.GetBytes(key);
            return Convert.ToHexString(key);
        }

        // use_weak_rsa_encryption_padding: typed with cast
        [HttpPost("rsa-sign")]
        public string RsaSign(string data)
        {
            using var rsa = RSA.Create(1024);
            var key = (RSA)rsa;
            return Convert.ToBase64String(
                key.SignData(Encoding.UTF8.GetBytes(data),
                    HashAlgorithmName.SHA1,
                    RSASignaturePadding.Pkcs1));
        }

        // timing-attack: variable named "token" compared with ==
        [HttpPost("check")]
        public IActionResult CheckToken(string token, string storedToken)
        {
            if (token == storedToken)
                return Ok("valid");
            return Unauthorized();
        }

        // toctou-file-exists-then-create
        [HttpPost("init")]
        public IActionResult InitFile(string filename)
        {
            var path = Path.Combine("/var/app/data", filename);
            if (!System.IO.File.Exists(path))
            {
                System.IO.File.WriteAllText(path, "{}");
            }
            return Ok(path);
        }

        // regular-expression-dos: no timeout, catastrophic backtracking pattern
        [HttpGet("validate")]
        public IActionResult ValidateInput(string input)
        {
            var r = new Regex(@"(a+)+$", RegexOptions.None);
            return Ok(r.IsMatch(input));
        }

        // regular-expression-dos-infinite-timeout: explicit infinite timeout
        [HttpGet("validate2")]
        public IActionResult ValidateInput2(string input)
        {
            var r = new Regex(@"([a-z]+)*", RegexOptions.None, Regex.InfiniteMatchTimeout);
            return Ok(r.IsMatch(input));
        }

        // XXE: xmldocument with XmlUrlResolver
        [HttpPost("parse-xml")]
        public IActionResult ParseXml(string xmlData)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.XmlResolver = new XmlUrlResolver();
            xmlDocument.LoadXml(xmlData);
            return Ok(xmlDocument.OuterXml);
        }

        // XXE: XmlReaderSettings with DtdProcessing.Parse
        [HttpPost("read-xml")]
        public IActionResult ReadXml(string xmlData)
        {
            var settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.XmlResolver = new XmlUrlResolver();
            using var reader = XmlReader.Create(new StringReader(xmlData), settings);
            while (reader.Read()) { }
            return Ok("parsed");
        }

        // XXE: XmlTextReader with DtdProcessing.Parse
        [HttpPost("textread-xml")]
        public IActionResult TextReadXml(string xmlData)
        {
            var reader = new XmlTextReader(new StringReader(xmlData));
            reader.DtdProcessing = DtdProcessing.Parse;
            while (reader.Read()) { }
            return Ok("read");
        }

        // razor-use-of-htmlstring: HtmlString with user content
        [HttpGet("html")]
        public IActionResult HtmlOutput(string content)
        {
            var html = new HtmlString(content);
            return Content(html.ToString(), "text/html");
        }

        // open-directory-listing
        [HttpGet("browse")]
        public IActionResult Browse(string dir)
        {
            var files = System.IO.Directory.GetFiles(dir);
            return Ok(files);
        }

        // missing-hsts-header: UseHsts missing, only UseHttpsRedirection
        // (captured in Program.cs — UseHsts already present there)

        // X509-subject-name-validation
        [HttpGet("cert-subject")]
        public IActionResult CertSubject(string expectedName)
        {
            var cert = new X509Certificate2("server.pfx", "pwd");
            if (cert.Subject.Contains(expectedName))
                return Ok("subject ok");
            return BadRequest("subject mismatch");
        }

        // X509Certificate2-privkey
        [HttpGet("cert-key")]
        public IActionResult CertPrivKey()
        {
            var cert = new X509Certificate2("server.pfx", "pwd",
                X509KeyStorageFlags.Exportable);
            var pk = cert.PrivateKey;
            return Ok(pk?.KeyExchangeAlgorithm);
        }
    }

    // missing-or-broken-authorization: controller with no [Authorize]
    [ApiController]
    [Route("api/admin-panel")]
    public class AdminPanelUnsecuredController : ControllerBase
    {
        [HttpGet("users")]
        public IActionResult GetAllUsers() => Ok("all users");

        [HttpDelete("user/{id}")]
        public IActionResult DeleteUser(int id) => Ok($"deleted {id}");

        [HttpGet("settings")]
        public IActionResult GetSettings() => Ok("settings");
    }

    // mvc-missing-antiforgery: POST action with no [ValidateAntiForgeryToken]
    [ApiController]
    [Route("api/form")]
    public class FormController : ControllerBase
    {
        [HttpPost("submit")]
        public IActionResult Submit([FromForm] string amount, [FromForm] string toAccount)
        {
            return Ok($"transferred {amount} to {toAccount}");
        }

        [HttpPost("change-password")]
        public IActionResult ChangePassword([FromForm] string newPassword)
        {
            return Ok("password changed");
        }
    }

    // misconfigured-lockout-option: lockout disabled
    [ApiController]
    [Route("api/identity")]
    public class IdentityConfigController : ControllerBase
    {
        [HttpPost("configure")]
        public IActionResult Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            services.AddDefaultIdentity<Microsoft.AspNetCore.Identity.IdentityUser>(options =>
            {
                options.Lockout.AllowedForNewUsers = false;
                options.Lockout.MaxFailedAccessAttempts = 100;
            });
            return Ok();
        }
    }
}
