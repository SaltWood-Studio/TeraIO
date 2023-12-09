using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Extension;

namespace TeraIO.Network.Http
{
    /// <summary>
    /// <see cref="HttpServer"/> 返回的 HttpServer 实例。调用 <see cref="HttpServerAppBase.Run"/> 来启动一个简单的 Http 服务器
    /// 正常情况下，你不应该创建这个类的实例，而是由 <see cref="HttpServer"/> 创建或者通过它的子类来获得它的实例！
    /// </summary>
    public class HttpServerAppBase
    {

        public string HOST => "127.0.0.1";
        public int PORT => 1280;

        public List<string> UriPrefixes {  get; set; }
        CancellationToken cancellationToken;
        public Dictionary<HttpHandlerFunction, MethodInfo> methods;

        public HttpServerAppBase(Dictionary<HttpHandlerFunction, MethodInfo>? methods = null)
        {
            this.cancellationToken = new CancellationToken();
            if (methods == null)
            {
                this.methods = new Dictionary<HttpHandlerFunction, MethodInfo>();
            }
            else
            {
                this.methods = methods;
            }
            this.UriPrefixes = new List<string>();
        }

        public void Run()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Merge(this.UriPrefixes);
            listener.Start();
            HandleHttpConnection(listener);
        }

        public HttpClientRequest ParseHttpRequest(HttpListenerContext context)
        {
            HttpClientRequest result = new HttpClientRequest
            {
                Method = context.Request.HttpMethod,
                Content = GetBytesFromStream(context.Request.InputStream),
                ContentType = context.Request.ContentType,
                Url = context.Request.RawUrl,
                Request = context.Request,
                Response = context.Response
            };
            return result;
        }

        public void HandleHttpConnection(HttpListener listener)
        {
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                Thread task = new Thread(() => HandleHttpConnectionSingle(context));
                task.Start();
            }
        }

        public void HandleHttpConnectionSingle(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            //Console.WriteLine(IsCorrectRoute(request.RawUrl, "/index"));
            MethodInfo[] methods = this.methods.Where(kvp => IsCorrectRoute(request.RawUrl, kvp.Key.Route)).Select(kvp => kvp.Value).OrderBy(f => f.Name.Length).Reverse().ToArray();
            if (methods != null)
            {
                foreach (MethodInfo method in methods)
                {
                    if (method != null)
                    {
                        //var a = Convert.ChangeType(this, this.GetType());
                        object[] parameters = new object[] { ParseHttpRequest(context) };
                        method.Invoke(this, parameters);
                        break;
                    }
                }
            }
            response.Close();
        }

        public string GetTemplateString(string name)
        {
            StreamReader sr = new StreamReader($"./templates/{name}");
            string t = sr.ReadToEnd();
            return t;
        }

        public byte[] GetTemplateByteArray(string name)
        {
            StreamReader sr = new StreamReader($"./templates/{name}");
            Stream t = sr.BaseStream;
            return GetBytesFromStream(t);
        }

        private bool IsCorrectRoute(string? raw, string? route)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(route))
            {
                return false;
            }
            if (raw.EndsWith('/') && raw.StartsWith(route.EndsWith('/') ? route : $"{route}/"))
            {
                return true;
            }
            if ((!raw.EndsWith('/')) && raw.StartsWith(route))
            {
                return true;
            }
            return false;
        }

        public byte[] GetBytesFromStream(Stream s)
        {
            byte[] buffer = new byte[4096];
            List<byte> result = new List<byte>();
            while (s.Read(buffer, 0, 4096) > 0)
            {
                result = result.Concat(buffer).ToList();
            }
            return result.ToArray();
        }
    }
}
