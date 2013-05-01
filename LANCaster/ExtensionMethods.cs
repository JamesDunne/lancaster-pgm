using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class ExtensionMethods
    {
        public static string F(this string format, params object[] args)
        {
            return String.Format(format, args);
        }
    }
}

namespace System.Net.Sockets
{
    public static class SocketExtensions
    {
        public static Task<Maybe<SocketError>> ConnectNonBlocking(this Socket s, EndPoint ep)
        {
            var tcs = new TaskCompletionSource<Maybe<SocketError>>();

            try
            {
                IAsyncResult r = s.BeginConnect(ep, iar =>
                {
                    try
                    {
                        s.EndConnect(iar);
                        tcs.SetResult(Maybe<SocketError>.Nothing);
                    }
                    catch (SocketException skex)
                    {
                        tcs.SetResult(skex.SocketErrorCode);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, (object)null);

                Debug.Assert(r != null);
            }
            catch (SocketException skex)
            {
                tcs.SetResult(skex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public static Task<Either<Socket, SocketError>> AcceptNonBlocking(this Socket s)
        {
            var tcs = new TaskCompletionSource<Either<Socket, SocketError>>();

            try
            {
                IAsyncResult r = s.BeginAccept(iar =>
                {
                    try
                    {
                        Socket rs = s.EndAccept(iar);
                        tcs.SetResult(rs);
                    }
                    catch (SocketException skex)
                    {
                        tcs.SetResult(skex.SocketErrorCode);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, (object)null);

                Debug.Assert(r != null);
            }
            catch (SocketException skex)
            {
                tcs.SetResult(skex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public static Task<Either<int, SocketError>> SendToNonBlocking(this Socket s, ArraySegment<byte> buf, SocketFlags flags, EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<Either<int, SocketError>>();

            try
            {
                IAsyncResult r = s.BeginSendTo(buf.Array, buf.Offset, buf.Count, flags, remoteEP, iar =>
                {
                    try
                    {
                        int n = s.EndSendTo(iar);
                        tcs.SetResult(n);
                    }
                    catch (SocketException skex)
                    {
                        tcs.SetResult(skex.SocketErrorCode);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, (object)null);

                Debug.Assert(r != null);
            }
            catch (SocketException skex)
            {
                tcs.SetResult(skex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public static Task<Either<int, SocketError>> SendNonBlocking(this Socket s, ArraySegment<byte> buf, SocketFlags flags)
        {
            SocketError err;

            var tcs = new TaskCompletionSource<Either<int, SocketError>>();

            try
            {
                IAsyncResult r = s.BeginSend(buf.Array, buf.Offset, buf.Count, flags, out err, iar =>
                {
                    try
                    {
                        SocketError errInner;
                        int n = s.EndSend(iar, out errInner);
                        if (n <= 0)
                            tcs.SetResult(errInner);
                        else
                            tcs.SetResult(n);
                    }
                    catch (SocketException skex)
                    {
                        tcs.SetResult(skex.SocketErrorCode);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, (object)null);

                if (r == null)
                    tcs.SetResult(err);
            }
            catch (SocketException skex)
            {
                tcs.SetResult(skex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        public static Task<Either<int, SocketError>> ReceiveNonBlocking(this Socket s, ArraySegment<byte> buf, SocketFlags flags)
        {
            SocketError err;

            var tcs = new TaskCompletionSource<Either<int, SocketError>>();

            try
            {
                IAsyncResult r = s.BeginReceive(buf.Array, buf.Offset, buf.Count, flags, out err, iar =>
                {
                    try
                    {
                        SocketError errInner;
                        int n = s.EndReceive(iar, out errInner);
                        if (n <= 0)
                            tcs.SetResult(errInner);
                        else
                            tcs.SetResult(n);
                    }
                    catch (SocketException skex)
                    {
                        tcs.SetResult(skex.SocketErrorCode);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }, (object)null);

                if (r == null)
                    tcs.SetResult(err);
            }
            catch (SocketException skex)
            {
                tcs.SetResult(skex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }
    }
}
