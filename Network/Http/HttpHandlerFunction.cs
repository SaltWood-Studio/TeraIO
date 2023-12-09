using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Network.Http
{
    public class HttpHandlerFunction : Attribute
    {
        public string Route { get; set; }
        public List<string> Methods { get; set; }

        public HttpHandlerFunction(string route, string methods = "GET")
        {
            this.Route = route;
            this.Methods = methods.Split(' ').ToList();
        }
    }
}
