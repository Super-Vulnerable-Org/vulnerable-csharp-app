using System;
using System.IO;
using System.Net;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExactMatchController : ControllerBase
    {
        // use_weak_rng_for_keygeneration: Random.NextBytes -> assigned to Aes.Key
        [HttpGet("aes-weak-key")]
        public string AesWithWeakKey(string data)
        {
            var rand = new System.Random();
            var keyBytes = new byte[16];
            System.Random rng = rand;
            rng.NextBytes(keyBytes);
            Aes cipher = Aes.Create();
            cipher.Key = keyBytes;
            var enc = cipher.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(enc.TransformFinalBlock(bytes, 0, bytes.Length));
        }

        // use_weak_rsa_encryption_padding: RSAPKCS1KeyExchangeFormatter
        [HttpPost("rsa-exchange")]
        public string RsaKeyExchange(string data)
        {
            using var rsa = RSA.Create(2048);
            var formatter = new RSAPKCS1KeyExchangeFormatter(rsa);
            var key = Encoding.UTF8.GetBytes(data.PadRight(16).Substring(0, 16));
            return Convert.ToBase64String(formatter.CreateKeyExchange(key));
        }

        // X509Certificate2-privkey: requires 'using System.Security.Cryptography' + X509Certificate2 var
        [HttpGet("cert-privkey")]
        public IActionResult GetCertPrivKey()
        {
            using System.Security.Cryptography;
            X509Certificate2 cert = new X509Certificate2("cert.pfx", "password");
            var key = cert.PrivateKey;
            return Ok(key?.KeyExchangeAlgorithm);
        }

        // X509-subject-name-validation: requires 'using System.IdentityModel.Tokens' + X509SecurityToken
        [HttpGet("token-subject")]
        public IActionResult ValidateTokenSubject(string rawToken)
        {
            using System.IdentityModel.Tokens;
            X509SecurityToken tok = new X509SecurityToken(new X509Certificate2("cert.pfx"));
            var subject = tok.Certificate.Subject;
            return Ok(subject);
        }

        // http-listener-wildcard: http://*:PORT/ prefix
        [HttpGet("listener")]
        public IActionResult StartWildcardListener()
        {
            using System.Net;
            var listener = new HttpListener();
            listener.Prefixes.Add("http://*:8080/");
            listener.Start();
            return Ok();
        }

        // XXE xmlreadersettings: exactly XmlReaderSettings RS = new; RS.DtdProcessing = DtdProcessing.Parse; XmlReader.Create(.., RS, ..)
        [HttpPost("xml-reader")]
        public IActionResult XmlReaderXxe(string xmlData)
        {
            XmlReaderSettings rs = new XmlReaderSettings();
            rs.DtdProcessing = DtdProcessing.Parse;
            rs.XmlResolver = new XmlUrlResolver();
            XmlReader reader = XmlReader.Create(new StringReader(xmlData), rs);
            while (reader.Read()) { }
            return Ok("ok");
        }

        // XXE xmltextreader: XmlTextReader without DtdProcessing.Prohibit
        [HttpPost("xml-text-reader")]
        public IActionResult XmlTextReaderXxe(string xmlData)
        {
            var reader = new XmlTextReader(new StringReader(xmlData));
            reader.WhitespaceHandling = WhitespaceHandling.None;
            while (reader.Read())
            {
                var val = reader.Value;
            }
            return Ok("ok");
        }

        // XPath injection: nav.Select("..." + input)
        [HttpGet("xpath-query")]
        public IActionResult XPathQuery(string username)
        {
            var doc = new XmlDocument();
            doc.LoadXml("<users><user><name>admin</name></user></users>");
            XPathNavigator nav = doc.CreateNavigator()!;
            XPathNodeIterator node = nav.Select("/users/user[name='" + username + "']");
            return Ok(node.Count);
        }

        // regular-expression-dos: public method, Regex var, then Match(input)
        [HttpGet("redos")]
        public IActionResult RegexDosExact(string input)
        {
            Regex r = new Regex(@"(a+)+$");
            var m = r.Match(input);
            return Ok(m.Success);
        }

        // regular-expression-dos-infinite-timeout
        [HttpGet("redos-infinite")]
        public IActionResult RegexDosInfiniteTimeout(string input)
        {
            var r = new Regex(@"([a-z]+)*", RegexOptions.None, TimeSpan.InfiniteMatchTimeout);
            return Ok(r.IsMatch(input));
        }
    }

    // missing-or-broken-authorization: inherits Controller (not ControllerBase) with no [Authorize]
    public class PaymentController : Controller
    {
        [HttpPost]
        public IActionResult ProcessPayment(string amount, string target)
        {
            return View("result");
        }
    }

    // mass-assignment: IActionResult method with unbound model passed to View()
    public class ProfileController : Controller
    {
        [HttpPost]
        public IActionResult UpdateProfile(UserProfile profile)
        {
            return View(profile);
        }
    }

    // mvc-missing-antiforgery: [HttpPost] IActionResult without [ValidateAntiForgeryToken]
    public class TransferFormController : Controller
    {
        [HttpPost]
        public IActionResult Transfer(string amount, string destination)
        {
            return View("result");
        }

        [HttpDelete]
        public IActionResult DeleteAccount(int id)
        {
            return View("deleted");
        }
    }

    public class UserProfile
    {
        public string Name { get; set; } = "";
        public bool IsAdmin { get; set; }
        public string Role { get; set; } = "";
    }
}
