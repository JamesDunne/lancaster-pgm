﻿using System;
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
        public bool UseNonBlockingIO = true;

        /// <summary>
        /// Default 80 MB/s send rate (= 80u * 1024u * 8u).
        /// </summary>
        public uint SendRateKBitsPerSec = 80u * 1024u * 8u;
        public TimeSpan SendWindowSize = TimeSpan.FromMilliseconds(250);
    }
}
