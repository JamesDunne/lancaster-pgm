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
        readonly int bufferSize;

        public Client(int bufferSize = 9040)
        {
            this.bufferSize = bufferSize;
            this.s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, PGM.IPPROTO_RM);
            this.s.ReceiveBufferSize = bufferSize * 2048;
        }

        public sealed class Connection
        {
            readonly Socket ls;
            readonly CancellationToken cancel;
            readonly int bufferSize;

            internal Connection(Socket ls, CancellationToken cancel, int bufferSize = 9040)
            {
                this.ls = ls;
                this.cancel = cancel;
                this.bufferSize = bufferSize;
            }

            public ArraySegment<byte> AllocateBuffer()
            {
                return new ArraySegment<byte>(new byte[bufferSize]);
            }

            public async Task<Either<ArraySegment<byte>, SocketError>> Receive(params ArraySegment<byte>[] bufs)
            {
                Debug.WriteLine("C: Receiving...");

                if (!ls.Connected) return SocketError.NotConnected;

                SocketError err = SocketError.Success;
                int n;

                try
                {
#if false
                    n = ls.Receive(bufs, SocketFlags.Partial, out err);
#else
                    n = await Task.Factory.FromAsync(
                        (AsyncCallback cb, object state) => ls.BeginReceive(bufs, SocketFlags.None, cb, state),
                        (IAsyncResult iar) => ls.EndReceive(iar, out err),
                        (object)null
                    );
#endif

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
                // TODO(jsd): Figure out proper shutdown procedure.
                ls.Shutdown(SocketShutdown.Both);
                ls.Close();
            }
        }

        public async Task<Connection> Accept(EndPoint ep, CancellationToken cancel)
        {
            s.Bind(ep);
            s.Listen(1);

            Debug.WriteLine("C: Accepting...");
            var ls = await Task.Factory.FromAsync(
                (AsyncCallback cb, object state) => s.BeginAccept(cb, state),
                (IAsyncResult r) => s.EndAccept(r),
                (object)null
            );
            Debug.WriteLine("C: Accepted");

            return new Connection(ls, cancel);
        }
    }
}
