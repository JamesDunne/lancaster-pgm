using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
            this.config = config = config ?? new ProtocolConfiguration();

            if (config.UsePGM)
            {
                this.s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, PGM.IPPROTO_RM);
            }
            else
            {
                this.s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            this.s.ReceiveBufferSize = config.BufferSize * 2048;

            this.s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (config.UseNonBlockingIO)
                this.s.UseOnlyOverlappedIO = true;
        }

        public async Task<Connection> Accept(CancellationToken cancel)
        {
            if (config.UsePGM)
            {
                s.Bind(config.MulticastEndPoint);
                s.Listen(1);

                Debug.WriteLine("C: Accepting...");
                var ls = await Task.Factory.FromAsync(
                    (AsyncCallback cb, object state) => s.BeginAccept(cb, state),
                    (IAsyncResult r) => s.EndAccept(r),
                    (object)null
                );
                Debug.WriteLine("C: Accepted");
                return new Connection(ls, cancel, config);
            }
            else
            {
                s.Bind(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)config.MulticastEndPoint).Port));
                s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(((IPEndPoint)config.MulticastEndPoint).Address));

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

            public async Task<Either<ArraySegment<byte>, SocketError>> Receive(params ArraySegment<byte>[] bufs)
            {
                Debug.WriteLine("C: Receiving...");

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
                        n = await Task.Factory.FromAsync(
                            (AsyncCallback cb, object state) => ls.BeginReceive(bufs, SocketFlags.None, cb, state),
                            (IAsyncResult iar) => ls.EndReceive(iar, out err),
                            (object)null
                        );
                    }
                    else
                    {
                        n = ls.Receive(bufs, SocketFlags.None, out err);
                    }

                    if (err != SocketError.Success || n == 0)
                        return err;
                }
                catch (SocketException skex)
                {
                    return skex.SocketErrorCode;
                }

                Debug.WriteLine("C: Received {0} bytes".F(n));
                Debug.Assert(n > 0);

                return new ArraySegment<byte>(bufs[0].Array, bufs[0].Offset, n);
            }

            public void Close()
            {
                Debug.WriteLine("C: Closing...");
                // TODO(jsd): Figure out proper shutdown procedure.
                //ls.Shutdown(SocketShutdown.Both);
                ls.Close();
                Debug.WriteLine("C: Closed");
            }
        }
    }
}
