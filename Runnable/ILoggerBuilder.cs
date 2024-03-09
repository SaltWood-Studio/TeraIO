using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Runnable
{
    public interface ILoggerBuilder
    {
        public ILogger GetLogger();

        public ILogger GetLogger(Type type);

        public ILogger GetLogger(string name);
    }
}
