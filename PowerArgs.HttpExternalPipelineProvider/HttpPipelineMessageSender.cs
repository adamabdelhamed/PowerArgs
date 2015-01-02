using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace PowerArgs.Preview
{
    public class HttpPipelineMessageSender
    {

        public int Port { get; private set; }
        public HttpPipelineMessageSender(int port)
        {
            this.Port = port;
        }

        public HttpPipelineControlResponse SendObject(object o)
        {
            HttpPipelineMessage message = new HttpPipelineMessage();
            message.PipedObjectJson = JsonConvert.SerializeObject(o, HttpPipelineMessage.CommonSettings);
            return SendRequest(message);
        }

        public HttpPipelineControlResponse SendControlAction(string action, params string[] parameters)
        {
            HttpPipelineMessage message = new HttpPipelineMessage();
            message.ControlAction = action;
            message.ControlParameters = parameters;
            return SendRequest(message);
        }

        private HttpPipelineControlResponse SendRequest(HttpPipelineMessage message)
        {

            var messageJson = JsonConvert.SerializeObject(message);
            var request = (HttpWebRequest)HttpWebRequest.Create("http://localhost:" + Port + "/");
            request.Method = "POST";
            using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(messageJson);
            }

            var response = (HttpWebResponse)request.GetResponse();

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                var responseBody = reader.ReadToEnd();
                var responseObj = JsonConvert.DeserializeObject<HttpPipelineControlResponse>(responseBody, HttpPipelineMessage.CommonSettings);
                return responseObj;
            }

        }
    }
}
