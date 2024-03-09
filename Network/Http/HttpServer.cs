using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Network.Http
{
    [Obsolete("请直接使用 HttpServerBase.Start")]
    public static class HttpServer
    {
        /// <summary>
        /// 通过一个继承自 <see cref="HttpServerAppBase"/> 的子类来简单创建一个 <see cref="HttpServer"/>
        /// </summary>
        /// <param name="app"></param>
        /// <returns>
        /// 生成的 <see cref="HttpServerAppBase"/> 对象,调用 <see cref="HttpServerAppBase.Run"/> 来启动 <see cref="HttpServer"/>
        /// </returns>
        public static HttpServerAppBase LoadNew(HttpServerAppBase app)
        {
            Type type = app.GetType();

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
            app.methods = methods;
            return app;
        }

        /// <summary>
        /// 通过一个 <see cref="MethodInfo"/>[] 来简单创建一个 <see cref="HttpServer"/>
        /// </summary>
        /// <param name="app"></param>
        /// <returns>
        /// 生成的 <see cref="HttpServerAppBase"/> 对象,调用 <see cref="HttpServerAppBase.Run"/> 来启动 <see cref="HttpServer"/>
        /// </returns>
        static HttpServerAppBase LoadNew(IList<MethodInfo> methodInfos)
        {
            HttpServerAppBase app = new HttpServerAppBase();
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
            app.methods = methods;
            return app;
        }
    }
}
