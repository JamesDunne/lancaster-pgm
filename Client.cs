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
        // PGM:
        readonly Socket s;

        public Client()
        {
            // PGM:
            this.s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, (ProtocolType)113);
        }

        public sealed class Connection
        {
            readonly Socket ls;
            readonly CancellationToken cancel;

            internal Connection(Socket ls, CancellationToken cancel)
            {
                this.ls = ls;
                this.cancel = cancel;
            }

            public async Task Read()
            {
                // Receive loop for the socket:

                byte[] buffer = new byte[65535];

                try
                {
                    while (!cancel.IsCancellationRequested)
                    {
                        Debug.WriteLine("C: Receiving...");

                        SocketError err = SocketError.Success;
                        var n = await Task.Factory.FromAsync(
                            (AsyncCallback cb, object state) => ls.BeginReceive(buffer, 0, 65535, SocketFlags.None, cb, state),
                            (IAsyncResult r) => ls.EndReceive(r, out err),
                            (object)null
                        );

                        if (err != SocketError.Success)
                        {
                            Debug.WriteLine("C: Closed due to {0}".F(err));
                            break;
                        }

                        Debug.WriteLine("C: Received");

                        if (n > 0)
                        {
                            Console.WriteLine("C: {0}", Encoding.ASCII.GetString(buffer, 0, n));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                }
            }
        }

        public async Task<Connection> Accept(EndPoint ep, CancellationToken cancel)
        {
            s.Bind(ep);
            s.Listen(10);

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
