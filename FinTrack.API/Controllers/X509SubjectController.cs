using System;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/x509subject")]
    public class X509SubjectController : ControllerBase
    {
        [HttpGet("validate")]
        public IActionResult ValidateSubjectName(string expectedIssuer)
        {
            X509Certificate2 cert = new X509Certificate2("server.pfx", "pwd");
            if (cert.SubjectName.Name == expectedIssuer)
                return Ok("issuer valid");
            return BadRequest("issuer mismatch");
        }

        [HttpGet("token")]
        public IActionResult ValidateTokenSubject()
        {
            X509SecurityToken tok = new X509SecurityToken(new X509Certificate2("cert.pfx"));
            if (tok.Certificate.SubjectName.Name == "CN=trusted.internal")
                return Ok("subject ok");
            return BadRequest("subject mismatch");
        }

        [HttpGet("getname")]
        public IActionResult GetCertName(string hostname)
        {
            X509Certificate2 cert = new X509Certificate2("cert.pfx");
            if (cert.SubjectName.Name != hostname)
                return BadRequest("host mismatch");
            return Ok("valid");
        }
    }
}
