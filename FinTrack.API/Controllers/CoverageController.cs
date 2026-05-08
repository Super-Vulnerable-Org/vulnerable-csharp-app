using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization.Formatters;
using System.Security.Cryptography.X509Certificates;
using System.Web.Script.Serialization;
using System.Web.UI;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using fastJSON;
using MBrace.FsPickler.Json;
using RazorEngine;

namespace FinTrack.API.Controllers
{
    // open-directory-listing: UseDirectoryBrowser inside Configure
    public class AppStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseDirectoryBrowser();
            app.UseRouting();
        }
    }

    // misconfigured-lockout-option: PasswordSignInAsync with lockoutOnFailure false (positional)
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private SignInManager<IdentityUser> _signIn;

        [HttpPost("login")]
        public async System.Threading.Tasks.Task<IActionResult> Login(string username, string password)
        {
            var result = await _signIn.PasswordSignInAsync(username, password, false, false);
            return Ok(result.Succeeded);
        }

        [HttpPost("check")]
        public async System.Threading.Tasks.Task<IActionResult> CheckLogin(string username, string password)
        {
            var user = await _signIn.UserManager.FindByNameAsync(username);
            var result = await _signIn.CheckPasswordSignInAsync(user, password, false);
            return Ok(result.Succeeded);
        }
    }

    // X509-subject-name-validation: uses SubjectName.Name comparison
    [ApiController]
    [Route("api/x509check")]
    public class X509CheckController : ControllerBase
    {
        [HttpGet("validate")]
        public IActionResult ValidateCertSubjectName(string expectedName)
        {
            X509Certificate2 cert = new X509Certificate2("server.pfx", "pwd");
            if (cert.SubjectName.Name == expectedName)
                return Ok("subject matches");
            return BadRequest("subject mismatch");
        }

        [HttpGet("token-subject")]
        public IActionResult ValidateTokenSubject()
        {
            using System.IdentityModel.Tokens;
            X509SecurityToken tok = new X509SecurityToken(new X509Certificate2("cert.pfx"));
            var name = tok.Certificate.SubjectName.Name;
            if (name == "CN=expected.host.com")
                return Ok("valid");
            return BadRequest("invalid");
        }
    }

    // toctou-file-exists-then-create: uses File.Exists (not System.IO.File.Exists)
    [ApiController]
    [Route("api/toctou")]
    public class ToctouController : ControllerBase
    {
        [HttpPost("init")]
        public IActionResult InitConfig(string configName)
        {
            var path = Path.Combine("/var/app/configs", configName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "{}");
            }
            return Ok(path);
        }

        [HttpPost("create")]
        public IActionResult CreateData(string name)
        {
            var path = Path.Combine("/var/app/data", name);
            if (!File.Exists(path))
            {
                File.Create(path);
            }
            return Ok(path);
        }

        [HttpDelete("remove")]
        public IActionResult RemoveFile(string name)
        {
            var path = Path.Combine("/var/app/data", name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return Ok(path);
        }
    }

    // xmltextreader-unsafe-defaults: typed XmlTextReader declaration (not var) for taint
    [ApiController]
    [Route("api/xmltextread")]
    public class XmlTextReadController : ControllerBase
    {
        [HttpPost("parse")]
        public string ParseXmlText(string xmlData)
        {
            XmlTextReader reader = new XmlTextReader(new StringReader(xmlData));
            reader.DtdProcessing = DtdProcessing.Parse;
            while (reader.Read())
            {
                var val = reader.Value;
            }
            return "parsed";
        }
    }

    // razor-template-injection: Razor.Parse with user-controlled ActionResult
    [ApiController]
    [Route("api/template")]
    public class TemplateRenderController : Controller
    {
        [HttpGet("render")]
        public ActionResult RenderTemplate(string userId, string template)
        {
            var result = Razor.Parse(template);
            return View((object)result);
        }

        [HttpPost("compile")]
        public ActionResult CompileView(string viewName, string template)
        {
            var output = Razor.Parse(template, new { Name = viewName });
            return View((object)output);
        }
    }

    // insecure-javascriptserializer-deserialization: new JavaScriptSerializer(new SimpleTypeResolver())
    [ApiController]
    [Route("api/jsdeser")]
    public class JavaScriptDeserController : ControllerBase
    {
        [HttpPost("deserialize")]
        public IActionResult Deserialize([FromBody] string json)
        {
            using System.Web.Script.Serialization;
            var resolver = new SimpleTypeResolver();
            var serializer = new JavaScriptSerializer(resolver);
            var obj = serializer.DeserializeObject(json);
            return Ok(obj?.ToString());
        }
    }

    // insecure-losformatter-deserialization: new LosFormatter()
    [ApiController]
    [Route("api/losdeser")]
    public class LosFormatterController : ControllerBase
    {
        [HttpPost("load")]
        public IActionResult LoadState([FromBody] string viewState)
        {
            using System.Web.UI;
            var formatter = new LosFormatter();
            var ms = new MemoryStream(Convert.FromBase64String(viewState));
            var obj = formatter.Deserialize(ms);
            return Ok(obj?.ToString());
        }
    }

    // insecure-typefilterlevel-full: BinaryServerFormatterSinkProvider.TypeFilterLevel = Full
    [ApiController]
    [Route("api/remoting")]
    public class RemotingController : ControllerBase
    {
        [HttpPost("setup")]
        public IActionResult SetupRemotingChannel()
        {
            BinaryServerFormatterSinkProvider sp = new BinaryServerFormatterSinkProvider();
            sp.TypeFilterLevel = TypeFilterLevel.Full;
            return Ok("remoting channel configured");
        }
    }

    // insecure-fastjson-deserialization: new JSONParameters { BadListTypeChecking = false }
    [ApiController]
    [Route("api/fastjson")]
    public class FastJsonController : ControllerBase
    {
        [HttpPost("deserialize")]
        public IActionResult FastJsonDeser([FromBody] string json)
        {
            using fastJSON;
            var settings = new JSONParameters
            {
                BadListTypeChecking = false
            };
            var obj = JSON.ToObject(json, settings);
            return Ok(obj?.ToString());
        }
    }

    // insecure-fspickler-deserialization: FsPickler.CreateJsonSerializer()
    [ApiController]
    [Route("api/pickler")]
    public class FsPicklerController : ControllerBase
    {
        [HttpPost("serialize")]
        public IActionResult SerializeData([FromBody] object data)
        {
            using MBrace.FsPickler.Json;
            var serializer = FsPickler.CreateJsonSerializer();
            var result = serializer.PickleToString(data);
            return Ok(result);
        }
    }
}
