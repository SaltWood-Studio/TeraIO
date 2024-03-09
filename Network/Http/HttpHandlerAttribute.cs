using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Network.Http
{
    public class HttpHandlerAttribute : Attribute
    {
        public string Route { get; set; }
        public List<string> Methods { get; set; }

        public HttpHandlerAttribute(string route, string methods = "GET")
        {
            this.Route = route;
            this.Methods = methods.Split(' ').ToList();
        }
    }
}
