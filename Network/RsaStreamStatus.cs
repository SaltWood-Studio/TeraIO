using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraIO.Network
{
    public enum RsaStreamStatus
    {
        NotStarted,
        Handshaking,
        Established,
        Closed,
        Failed = 100,
        AuthenticationFailed = 101
    }
}
