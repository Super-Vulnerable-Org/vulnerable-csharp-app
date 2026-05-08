using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class X509Controller : ControllerBase
    {
        [HttpGet("validate")]
        public IActionResult ValidateCertificate(string host)
        {
            var cert = new X509Certificate2("cert.pfx", "password");
            var chain = new X509Chain();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            var valid = chain.Build(cert);
            return Ok(new { valid, subject = cert.Subject });
        }

        [HttpGet("name-check")]
        public IActionResult CheckSubjectName(string expectedHost)
        {
            var cert = new X509Certificate2("cert.pfx", "password");
            if (cert.Subject.Contains(expectedHost))
                return Ok("cert matches host");
            return BadRequest("subject name mismatch");
        }

        [HttpGet("privkey")]
        public IActionResult ExportPrivKey()
        {
            var cert = new X509Certificate2("cert.pfx", "password",
                X509KeyStorageFlags.Exportable);
            var privKey = cert.PrivateKey;
            return Ok(privKey?.KeyExchangeAlgorithm);
        }
    }
}
