using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LANCaster
{
    public sealed class ProtocolConfiguration
    {
        public int BufferSize = 9040;
        public bool UsePGM = true;
        public bool UseNonBlockingIO = true;
        public EndPoint MulticastEndPoint;
    }
}
