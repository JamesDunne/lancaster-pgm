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
            s.UseOnlyOverlappedIO = true;

            s.Bind(new IPEndPoint(IPAddress.Any, 0));

            // NOTE(jsd): This option fails no matter what.
            //s.SetSocketOption(PGM.IPPROTO_RM, PGM.RM_SEND_WINDOW_ADV_RATE, 50);
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

        public async Task<Either<int, SocketError>> Send(params ArraySegment<byte>[] bufs)
        {
            Debug.WriteLine("S: Sending...");
            SocketError err = SocketError.Success;
#if true
            int snv = s.Send(bufs, SocketFlags.None, out err);
#else
            int snv = await Task.Factory.FromAsync(
                (AsyncCallback cb, object state) => s.BeginSend(bufs, SocketFlags.None, cb, state),
                (IAsyncResult iar) => s.EndSend(iar, out err),
                (object)null
            ).ConfigureAwait(false);
#endif
            if (err != SocketError.Success)
                return err;

            Debug.WriteLine("S: Sent");
            return snv;
        }

        public void Close()
        {
            Debug.WriteLine("S: Closing...");
            //s.Shutdown(SocketShutdown.Both);
            s.Close();
            Debug.WriteLine("S: Closed");
        }
    }
}
