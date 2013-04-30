using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LANCaster
{
    public static class PGM
    {
        public struct ProtocolTypeValue
        {
            readonly int _value;

            public ProtocolTypeValue(int value)
            {
                _value = value;
            }

            public static implicit operator ProtocolType(ProtocolTypeValue t)
            {
                return (ProtocolType)t._value;
            }

            public static implicit operator SocketOptionLevel(ProtocolTypeValue t)
            {
                return (SocketOptionLevel)t._value;
            }
        }
        
        public struct SocketOptionNameValue
        {
            readonly int _value;

            public SocketOptionNameValue(int value)
            {
                _value = value;
            }

            public static implicit operator SocketOptionName(SocketOptionNameValue t)
            {
                return (SocketOptionName)t._value;
            }
        }

        public static readonly ProtocolTypeValue IPPROTO_RM = new ProtocolTypeValue(113);

        [StructLayout(LayoutKind.Sequential)]
        public struct RMSendWindow
        {
            public uint RateKbitsPerSec;
            public uint WindowSizeInMilliSecs;
            public uint WindowSizeInBytes;

            /// <summary>
            /// windowSizeInBytes = (rateKbitsPerSec / 8) * windowSizeInMilliSecs
            /// </summary>
            /// <param name="rateKbitsPerSec"></param>
            /// <param name="windowSizeInMilliSecs"></param>
            public RMSendWindow(
                uint rateKbitsPerSec,
                uint windowSizeInMilliSecs
            )
            {
                RateKbitsPerSec = rateKbitsPerSec;
                WindowSizeInMilliSecs = windowSizeInMilliSecs;
                WindowSizeInBytes = (rateKbitsPerSec / 8) * windowSizeInMilliSecs;
            }

            public static implicit operator byte[](RMSendWindow sw)
            {
                return StructureToByteArray(sw);
            }
        }

        public static readonly SocketOptionNameValue RM_RATE_WINDOW_SIZE = new SocketOptionNameValue(1001);
        public static readonly SocketOptionNameValue RM_SET_MESSAGE_BOUNDARY = new SocketOptionNameValue(1002);
        public static readonly SocketOptionNameValue RM_FLUSHCACHE = new SocketOptionNameValue(1003);
        public static readonly SocketOptionNameValue RM_SENDER_WINDOW_ADVANCE_METHOD = new SocketOptionNameValue(1004);
        public static readonly SocketOptionNameValue RM_SENDER_STATISTICS = new SocketOptionNameValue(1005);
        public static readonly SocketOptionNameValue RM_LATEJOIN = new SocketOptionNameValue(1006);
        public static readonly SocketOptionNameValue RM_SET_SEND_IF = new SocketOptionNameValue(1007);
        public static readonly SocketOptionNameValue RM_ADD_RECEIVE_IF = new SocketOptionNameValue(1008);
        public static readonly SocketOptionNameValue RM_DEL_RECEIVE_IF = new SocketOptionNameValue(1009);
        public static readonly SocketOptionNameValue RM_SEND_WINDOW_ADV_RATE = new SocketOptionNameValue(1010);
        public static readonly SocketOptionNameValue RM_USE_FEC = new SocketOptionNameValue(1011);
        public static readonly SocketOptionNameValue RM_SET_MCAST_TTL = new SocketOptionNameValue(1012);
        public static readonly SocketOptionNameValue RM_RECEIVER_STATISTICS = new SocketOptionNameValue(1013);
        public static readonly SocketOptionNameValue RM_HIGH_SPEED_INTRANET_OPT = new SocketOptionNameValue(1014);

        /// <summary>
        /// Method to convert a struct to a byte[] for marshalling.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static byte[] StructureToByteArray(object obj)
        {
            int length = Marshal.SizeOf(obj);
            byte[] result = new byte[length];
            IntPtr p = Marshal.AllocHGlobal(length);
            Marshal.StructureToPtr(obj, p, true);
            Marshal.Copy(p, result, 0, length);
            Marshal.FreeHGlobal(p);
            return result;
        }

        /// <summary>
        /// Detects if PGM support is installed.
        /// </summary>
        /// <returns>true if installed, false otherwise</returns>
        public static bool Detect()
        {
            try
            {
                // TODO(jsd): Use @"SOFTWARE\Microsoft\MSMQ\Parameters\Setup" for XP/2003
                var setupKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSMQ\Setup");
                if (setupKey == null) return false;

                // Introduced in MSMQ 4.0:
                object value1 = setupKey.GetValue(@"msmq_MulticastInstalled");
                if (value1 != null)
                {
                    if ((int)value1 != 0) return true;
                }

                // Fallback to old key name:
                object value2 = setupKey.GetValue(@"msmq_Multicast");
                if (value2 != null)
                {
                    if ((int)value2 != 0) return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
