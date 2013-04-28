using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LANCaster
{
    public static class PGM
    {
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
