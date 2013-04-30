using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LANCaster
{
    public sealed class Server
    {
        readonly ProtocolConfiguration config;
        readonly Socket s;

        public Server(ProtocolConfiguration config)
        {
            if (config == null) throw new ArgumentNullException("config");
            this.config = config;

            // PGM or UDP:
            if (config.UsePGM)
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Rdm, PGM.IPPROTO_RM);
            }
            else
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            if (config.UseNonBlockingIO)
                s.UseOnlyOverlappedIO = true;

            if (config.UseLoopback)
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Calculate send window buffer size:
            var sendWindow = new PGM.RMSendWindow(config.SendRateKBitsPerSec, config.SendWindowSize);

            if (config.UsePGM)
            {
                // PGM:
                s.Bind(new IPEndPoint(IPAddress.Any, config.MulticastEndpoint.Port));

                // NOTE(jsd): This option fails here:
                //s.SetSocketOption(PGM.IPPROTO_RM, PGM.RM_SEND_WINDOW_ADV_RATE, 50);

                // Set the window size parameters (very important for good bandwidth utilization):
                s.SetSocketOption(PGM.IPPROTO_RM, PGM.RM_RATE_WINDOW_SIZE, sendWindow);
            }
            else
            {
                // Multicast UDP:
                s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, (int)sendWindow.WindowSizeInBytes);

                if (config.UseLoopback)
                {
                    s.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
                }
            }
        }

        public async Task Connect(CancellationToken cancel)
        {
            Debug.WriteLine("S: Connecting...");

            if (config.UsePGM)
            {
                if (config.UseNonBlockingIO)
                {
                    await Task.Factory.FromAsync(
                        (AsyncCallback cb, object state) => s.BeginConnect(config.MulticastEndpoint, cb, state),
                        (IAsyncResult iar) => s.EndConnect(iar),
                        (object)null
                    );
                }
                else
                {
                    s.Connect(config.MulticastEndpoint);
                }
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
            }

            Debug.WriteLine("S: Connected");
        }

        public async Task<Either<int, SocketError>> Send(ArraySegment<byte> buf)
        {
            Debug.WriteLine("S: Sending...");
            SocketError err = SocketError.Success;

            int snv;
            try
            {
                if (config.UsePGM)
                {
                    if (config.UseNonBlockingIO)
                    {
#if true
                        var res = await s.SendAsTask(buf, SocketFlags.None);
                        if (res.IsRight) return res.Right;

                        snv = res.Left;
#else
                        snv = await Task.Factory.FromAsync(
                            (AsyncCallback cb, object state) => s.BeginSend(buf.Array, buf.Offset, buf.Count, SocketFlags.None, cb, state),
                            (IAsyncResult iar) => s.EndSend(iar, out err),
                            (object)null
                        );
#endif
                    }
                    else
                    {
                        snv = s.Send(buf.Array, buf.Offset, buf.Count, SocketFlags.None, out err);
                    }
                }
                else
                {
                    if (config.UseNonBlockingIO)
                    {
#if true
                        var res = await s.SendToAsTask(buf, SocketFlags.None, config.MulticastEndpoint);
                        if (res.IsRight) return res.Right;

                        snv = res.Left;
#else
                        snv = await Task.Factory.FromAsync(
                            (AsyncCallback cb, object state) => s.BeginSendTo(buf.Array, buf.Offset, buf.Count, SocketFlags.None, config.MulticastEndpoint, cb, state),
                            (IAsyncResult iar) => s.EndSendTo(iar),
                            (object)null
                        );
#endif
                    }
                    else
                    {
                        snv = s.SendTo(buf.Array, buf.Offset, buf.Count, SocketFlags.None, config.MulticastEndpoint);
                    }
                }

                if (err != SocketError.Success || snv <= 0)
                    return err;
            }
            catch (SocketException skex)
            {
                return skex.SocketErrorCode;
            }

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
