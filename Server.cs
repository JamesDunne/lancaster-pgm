using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LANCaster
{
    public sealed class Server
    {
        readonly int bufferSize;
        readonly Socket s;

        public Server(int bufferSize = 9040)
        {
            this.bufferSize = bufferSize;

            s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, PGM.IPPROTO_RM);

            s.Bind(new IPEndPoint(IPAddress.Any, 0));

            s.SetSocketOption(PGM.IPPROTO_RM, PGM.RM_RATE_WINDOW_SIZE, new PGM.RMSendWindow(24000u, 0u, 64u * 1024u * 1024u));
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        public async Task Connect(EndPoint ep, CancellationToken cancel)
        {
            Debug.WriteLine("S: Connecting...");
            await Task.Factory.FromAsync(
                (AsyncCallback cb, object state) => s.BeginConnect(ep, cb, state),
                (IAsyncResult iar) => s.EndConnect(iar),
                (object)null
            );
            Debug.WriteLine("S: Connected");
        }

        public async Task<Either<int, SocketError>> Send()
        {
            byte[] buffer = new byte[bufferSize];
            int sn = Encoding.ASCII.GetBytes("HELLO", 0, 5, buffer, 0);

            Debug.WriteLine("S: Sending...");
            SocketError err = SocketError.Success;
            int snv = await Task.Factory.FromAsync(
                (AsyncCallback cb, object state) => s.BeginSend(buffer, 0, sn, SocketFlags.None, cb, state),
                (IAsyncResult iar) => s.EndSend(iar, out err),
                (object)null
            );
            if (err != SocketError.Success)
                return err;

            if (snv != sn)
            {
                Console.Error.WriteLine("S: {0} != {1}", snv, sn);
            }

            Debug.WriteLine("S: Sent");
            return snv;
        }

        public void Close()
        {
            s.Shutdown(SocketShutdown.Both);
            s.Close();
        }
    }
}
