using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Web.Script.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FinTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeserializationController : ControllerBase
    {
        [HttpPost("soap")]
        public IActionResult SoapDeser([FromBody] byte[] data)
        {
            var formatter = new SoapFormatter();
            using var ms = new MemoryStream(data);
            var obj = formatter.Deserialize(ms);
            return Ok(obj?.ToString());
        }

        [HttpPost("netdc")]
        public IActionResult NetDcDeser([FromBody] Stream body)
        {
            var ser = new NetDataContractSerializer();
            var obj = ser.Deserialize(body);
            return Ok(obj?.ToString());
        }

        [HttpPost("jsser")]
        public IActionResult JsSerDeser([FromBody] string json)
        {
            var ser = new JavaScriptSerializer();
            ser.RegisterConverters(new[] { new SimpleTypeConverter() });
            var obj = ser.DeserializeObject(json);
            return Ok(obj?.ToString());
        }

        [HttpPost("losformat")]
        public IActionResult LosDeser([FromBody] string encoded)
        {
            var formatter = new System.Web.UI.LosFormatter();
            var obj = formatter.Deserialize(encoded);
            return Ok(obj?.ToString());
        }

        [HttpPost("typelevel")]
        public IActionResult TypeLevelDeser([FromBody] Stream body)
        {
            var channel = new TcpChannel();
            var sink = channel.CreateMessageSink(null, new System.Collections.Hashtable
            {
                ["typeFilterLevel"] = "Full"
            }, out _);
            return Ok();
        }

        [HttpPost("datacontract")]
        public IActionResult DataContractDeser([FromBody] Stream body)
        {
            var resolver = new MyDataContractResolver();
            var ser = new DataContractSerializer(typeof(object), new DataContractSerializerSettings
            {
                DataContractResolver = resolver
            });
            var obj = ser.ReadObject(body);
            return Ok(obj?.ToString());
        }

        [HttpPost("newtonsoft-type")]
        public IActionResult NewtonsoftTypeHandling([FromBody] string json)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            var obj = JsonConvert.DeserializeObject(json, settings);
            return Ok(obj?.ToString());
        }
    }

    public class MyDataContractResolver : DataContractResolver
    {
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
            => Type.GetType(typeName + ", " + typeNamespace);
        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out System.Xml.XmlDictionaryString typeName, out System.Xml.XmlDictionaryString typeNamespace)
        {
            typeName = null; typeNamespace = null; return false;
        }
    }

    public class SimpleTypeConverter : JavaScriptConverter
    {
        public override System.Collections.Generic.IEnumerable<Type> SupportedTypes => new[] { typeof(object) };
        public override object Deserialize(System.Collections.Generic.IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer) => dictionary;
        public override System.Collections.Generic.IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer) => new System.Collections.Generic.Dictionary<string, object>();
    }
}
