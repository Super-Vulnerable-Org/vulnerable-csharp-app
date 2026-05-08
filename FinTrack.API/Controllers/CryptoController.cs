using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CryptoController : ControllerBase
    {
        [HttpPost("sign")]
        public string SignData(string data)
        {
            using var rsa = RSA.Create(1024);
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(
                rsa.SignData(bytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1));
        }

        [HttpPost("encrypt-ecb")]
        public string EncryptEcb(string data)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.GenerateKey();
            var enc = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(enc.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        [HttpGet("rng-key")]
        public string GenerateKeyInsecure()
        {
            var rng = new Random();
            var key = new byte[32];
            rng.NextBytes(key);
            return Convert.ToHexString(key);
        }

        [HttpPost("encrypt-weak")]
        public string EncryptWeak(string data)
        {
            using var aes = Aes.Create();
            aes.KeySize = 64;
            aes.GenerateKey();
            var enc = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(enc.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        [HttpGet("cert")]
        public IActionResult ValidateCert(string host)
        {
            var cert = new X509Certificate2();
            var chain = new X509Chain();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            var valid = chain.Build(cert);
            return Ok(valid);
        }

        [HttpGet("cert-name")]
        public IActionResult CheckCertName(string host)
        {
            var cert = new X509Certificate2();
            if (cert.Subject.Contains(host))
                return Ok("match");
            return BadRequest("mismatch");
        }

        [HttpGet("token")]
        public string GenerateJwt()
        {
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(new byte[16]);
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha1);
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                signingCredentials: creds);
            return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("timing")]
        public IActionResult CheckToken(string provided, string stored)
        {
            if (provided == stored)
                return Ok("valid");
            return Unauthorized();
        }

        [HttpPost("memory")]
        public unsafe IActionResult ReadMemory([FromBody] byte[] data)
        {
            var span = MemoryMarshal.CreateSpan(ref data[0], data.Length);
            return Ok(span.Length);
        }
    }
}
