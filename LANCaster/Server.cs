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

        public async Task<Maybe<SocketError>> Connect(CancellationToken cancel)
        {
            try
            {
                if (config.UsePGM)
                {
                    if (config.UseNonBlockingIO)
                    {
                        return await s.ConnectNonBlocking(config.MulticastEndpoint);
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
            }
            catch (SocketException skex)
            {
                return skex.SocketErrorCode;
            }

            return Maybe<SocketError>.Nothing;
        }

        public async Task<Either<int, SocketError>> Send(ArraySegment<byte> buf)
        {
            SocketError err = SocketError.Success;

            int snv;
            try
            {
                if (config.UsePGM)
                {
                    if (config.UseNonBlockingIO)
                    {
                        var res = await s.SendNonBlocking(buf, SocketFlags.None);
                        if (res.IsRight) return res.Right;

                        snv = res.Left;
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
                        var res = await s.SendToNonBlocking(buf, SocketFlags.None, config.MulticastEndpoint);
                        if (res.IsRight) return res.Right;

                        snv = res.Left;
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

            return snv;
        }

        public void Close()
        {
            //s.Shutdown(SocketShutdown.Both);
            s.Close();
        }
    }
}
