using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TeraIO.Data;

namespace TeraIO.Network.Http
{
    public struct HttpClientRequest
    {
        public string? Url { get; set; }
        public string? ContentType { get; set; }
        public string Method { get; set; }
        public byte[] Content { get; set; }
        public HttpListenerRequest Request { get; set; }
        public HttpListenerResponse Response { get; set; }
        public Stream InputStream { get => this.Request.InputStream; }
        public Stream OutputStream { get => this.Response.OutputStream; }
        public int ResponseStatusCode { get => this.Response.StatusCode; set => this.Response.StatusCode = value; }

        public T? TryParseTo<T>() where T : class
        {
            T? result = null;
            try
            {
                result = JsonConvert.DeserializeObject<T>(this.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public void WriteFrom(Stream stream) => stream.CopyTo(this.Response.OutputStream);

        public T ParseTo<T>() where T : class
        {
            T? result = JsonConvert.DeserializeObject<T>(this.Text);
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            return result;
        }

        public int Send(string content)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            this.OutputStream.Write(bytes);
            return bytes.Length;
        }

        public int Send(byte[] bytes)
        {
            this.OutputStream.Write(bytes);
            return bytes.Length;
        }

        public string Text
        {
            get
            {
                return Encoding.UTF8.GetString(Content);
            }
        }
    }
}
