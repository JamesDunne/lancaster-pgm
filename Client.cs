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

            // Calculate send window buffer size:
            var sendWindow = new PGM.RMSendWindow(config.SendRateKBitsPerSec, config.SendWindowSize);

            this.s.ReceiveBufferSize = (int)sendWindow.WindowSizeInBytes;

            this.s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (config.UseNonBlockingIO)
                this.s.UseOnlyOverlappedIO = true;
        }

        public async Task<Connection> Accept(CancellationToken cancel)
        {
            if (config.UsePGM)
            {
                s.Bind(config.MulticastEndpoint);
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
                            (AsyncCallback cb, object state) => ls.BeginReceive(buf.Array, buf.Offset, buf.Count, SocketFlags.None, cb, state),
                            (IAsyncResult iar) => ls.EndReceive(iar, out err),
                            (object)null
                        );
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

                Debug.WriteLine("C: Received {0} bytes".F(n));
                Debug.Assert(n > 0);

                return new ArraySegment<byte>(buf.Array, buf.Offset, n);
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
