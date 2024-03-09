using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TeraIO.Extension;
using TeraIO.Runnable;

namespace TeraIO.Network.Http
{
    /// <summary>
    /// <see cref="HttpServer"/> 返回的 HttpServer 实例。调用 <see cref="HttpServerAppBase.Run"/> 来启动一个简单的 Http 服务器
    /// 正常情况下,你不应该创建这个类的实例,而是由 <see cref="HttpServer"/> 创建或者通过它的子类来获得它的实例！
    /// </summary>
    public class HttpServerAppBase : RunnableBase
    {

        public List<string> UriPrefixes { get; set; }
        public Dictionary<HttpHandlerAttribute, MethodInfo> methods;

        protected HttpListener listener;

        public HttpServerAppBase(Dictionary<HttpHandlerAttribute, MethodInfo>? methods = null, ILoggerBuilder? loggerBuilder = null) : base(loggerBuilder)
        {
            if (methods == null)
            {
                this.methods = new Dictionary<HttpHandlerAttribute, MethodInfo>();
            }
            else
            {
                this.methods = methods;
            }
            this.UriPrefixes = new List<string>();
            listener = new HttpListener();
        }

        protected void LoadNew()
        {
            Type type = this.GetType();

            // 获取所有被标记为HttpHandlerFunction的方法
            MethodInfo[] methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Dictionary<HttpHandlerAttribute, MethodInfo> methods = new();

            // 遍历方法数组,检查每个方法是否被标记为HttpHandlerFunction
            foreach (MethodInfo methodInfo in methodInfos)
            {
                HttpHandlerAttribute? attr = Attribute.GetCustomAttribute(methodInfo, typeof(HttpHandlerAttribute)) as HttpHandlerAttribute;
                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (attr != null && parameters.Length > 0 && parameters[0].ParameterType == typeof(HttpClientRequest))
                {
                    methods.Add(attr, methodInfo);
                }
            }
            this.methods = methods;
        }

        protected override int Run(string[] args)
        {
            int result = 0;
            this.LoadNew();
            //listener.Prefixes.Clear();
            listener.Prefixes.Merge(UriPrefixes);
            listener.Start();
            HandleHttpConnection(listener);
            return result;
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
                Task task = Task.Run(() => HandleHttpConnectionSingle(context));
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
