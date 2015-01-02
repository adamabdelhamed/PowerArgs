using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace PowerArgs.Preview
{
    public class HttpPipelineMessage
    {
        public string PipedObjectJson { get; set; }
        public string ControlAction { get; set; }
        public string[] ControlParameters { get; set; }

        public static readonly JsonSerializerSettings CommonSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All, TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple };

        public T DeserializetPipedObject<T>()
        {
            return JsonConvert.DeserializeObject<T>(PipedObjectJson, CommonSettings);
        }
    }

    public class HttpPipelineControlResponse
    {
        public string Value { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string PipedObjectArrayJson { get; set; }

        public List<ConsoleCharacter> ConsoleOutput { get; set; }

        public string ExceptionInfo { get; set; }

        [JsonIgnore]
        public bool Close { get; set; }
    }
}
