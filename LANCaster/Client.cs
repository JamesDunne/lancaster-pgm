﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LANCaster
{
    public sealed class Client
    {
        readonly Socket s;
        readonly ProtocolConfiguration config;

        public Client(ProtocolConfiguration config)
        {
            if (config == null) throw new ArgumentNullException("config");
            this.config = config;

            if (config.UsePGM)
            {
                this.s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, PGM.IPPROTO_RM);
            }
            else
            {
                this.s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            this.s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Calculate send window buffer size:
            var sendWindow = new PGM.RMSendWindow(config.SendRateKBitsPerSec, config.SendWindowSize);

            this.s.ReceiveBufferSize = (int)sendWindow.WindowSizeInBytes;

            if (config.UseNonBlockingIO)
                this.s.UseOnlyOverlappedIO = true;
        }

        public async Task<Either<Connection, SocketError>> Accept(CancellationToken cancel)
        {
            if (config.UsePGM)
            {
                s.Bind(config.MulticastEndpoint);
                s.Listen(1);

                var res = await s.AcceptNonBlocking();
                return res.Collapse<Either<Connection, SocketError>>(
                    ls => new Connection(ls, cancel, config),
                    err => err
                );
            }
            else
            {
                if (config.UseLoopback)
                {
                    s.Bind(new IPEndPoint(IPAddress.Loopback, config.MulticastEndpoint.Port));
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                }
                else
                {
                    s.Bind(config.MulticastEndpoint);
                }
                s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(config.MulticastEndpoint.Address));

                s.ReceiveTimeout = 5000;
                return new Connection(s, cancel, config);
            }
        }

        public sealed class Connection
        {
            readonly Socket ls;
            readonly CancellationToken cancel;
            readonly ProtocolConfiguration config;

            internal Connection(Socket ls, CancellationToken cancel, ProtocolConfiguration config)
            {
                this.ls = ls;
                this.cancel = cancel;
                this.config = config;
            }

            public ArraySegment<byte> AllocateBuffer()
            {
                return new ArraySegment<byte>(new byte[config.BufferSize]);
            }

            public async Task<Either<ArraySegment<byte>, SocketError>> Receive(ArraySegment<byte> buf)
            {
                if (config.UsePGM)
                {
                    if (!ls.Connected)
                        return SocketError.NotConnected;
                }

                SocketError err = SocketError.Success;
                int n;

                try
                {
                    // NOTE(jsd): Logic is equivalent regardless of PGM or UDP protocol:
                    if (config.UseNonBlockingIO)
                    {
                        var res = await ls.ReceiveNonBlocking(buf, SocketFlags.None);
                        if (res.IsRight) return res.Right;

                        n = res.Left;
                    }
                    else
                    {
                        n = ls.Receive(buf.Array, buf.Offset, buf.Count, SocketFlags.None, out err);
                    }

                    if (err != SocketError.Success || n == 0)
                        return err;
                }
                catch (SocketException skex)
                {
                    return skex.SocketErrorCode;
                }

                Debug.Assert(n > 0);

                return new ArraySegment<byte>(buf.Array, buf.Offset, n);
            }

            public void Close()
            {
                // TODO(jsd): Figure out proper shutdown procedure.
                //ls.Shutdown(SocketShutdown.Both);
                ls.Close();
            }
        }
    }
}
