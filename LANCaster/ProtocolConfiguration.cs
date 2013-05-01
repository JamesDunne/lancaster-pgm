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
        // Required fields:
        public readonly IPEndPoint MulticastEndpoint;

        public ProtocolConfiguration(IPEndPoint multicastEndpoint)
        {
            if (multicastEndpoint == null) throw new ArgumentNullException("multicastEndpoint");

            MulticastEndpoint = multicastEndpoint;
        }

        // User-modifiable fields:

        public int BufferSize = 9040;
        public bool UsePGM = true;
        public bool UseLoopback = false;
        public bool UseNonBlockingIO = true;

        /// <summary>
        /// Default 100 MB/s send rate (= 100u * 1024u * 8u).
        /// </summary>
        public uint SendRateKBitsPerSec = 100u * 1024u * 8u;
        public TimeSpan SendWindowSize = TimeSpan.FromMilliseconds(250);

        public static readonly ProtocolConfiguration Default = new ProtocolConfiguration(new IPEndPoint(IPAddress.Parse("224.12.19.82"), 1982))
        {
            UsePGM = true,
            UseLoopback = false,
            UseNonBlockingIO = true
        };
    }
}
