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
    public sealed class Server
    {
        readonly Socket s;

        public Server()
        {
            // 113 is SocketType.Rm, were it defined:
            this.s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, (ProtocolType)113);
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

        public async Task Send()
        {
            byte[] buffer = new byte[65535];
            int sn = Encoding.ASCII.GetBytes("HELLO", 0, 5, buffer, 0);

            Debug.WriteLine("S: Sending...");
            var snv = await Task.Factory.FromAsync(
                (AsyncCallback cb, object state) => s.BeginSend(buffer, 0, sn, SocketFlags.None, cb, state),
                (IAsyncResult iar) => s.EndSend(iar),
                (object)null
            );
            Debug.WriteLine("S: Sent");
        }

        public void Close()
        {
            s.Close();
        }
    }
}
