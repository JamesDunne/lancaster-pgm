﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LANCaster;

namespace LANCasterClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Run the main task and wait for its completion:
            Task.Run((Func<Task>)Run).Wait();
        }

        static async Task Run()
        {
            var config = ProtocolConfiguration.Default;

            // Make sure that PGM is installed if required:
            if (config.UsePGM && !PGM.Detect())
            {
                Console.Error.WriteLine("PGM protocol is required to be installed.");
                Console.Error.WriteLine("To install PGM, open 'Turn Windows features on or off' in Control Panel and enable feature 'Microsoft Message Queue (MSMQ) Server > Server Core > Multicasting Support'.");
                Environment.ExitCode = 1;
                return;
            }

#if false
            // Wait for keypress to start:
            while (Console.KeyAvailable) Console.ReadKey(true);
            Console.WriteLine("Press any key to start...");
            Console.ReadKey(true);
#endif

            try
            {
                var client = new Client(config);

                // Client listen needs some data sent to accept:
                Console.WriteLine("Accepting...");
                var res = await client.Accept(CancellationToken.None);
                if (res.IsRight)
                {
                    Console.Error.WriteLine(res.Right);
                    return;
                }
                var conn = res.Left;
                Console.WriteLine("Accepted");

                var buf = conn.AllocateBuffer();
                while (true)
                {
                    // Read data into our buffer:
                    var rres = await conn.Receive(buf);
                    if (rres.IsRight)
                    {
                        if (!config.UsePGM)
                        {
                            if (rres.Right == System.Net.Sockets.SocketError.NotConnected) continue;
                        }

                        Console.Error.WriteLine("{0}", rres.Right);
                        conn.Close();
                        break;
                    }

                    var rbuf = rres.Left;
                    int k = BitConverter.ToInt32(rbuf.Array, rbuf.Offset);
                    if ((k & 511) == 0)
                    {
                        // , Encoding.ASCII.GetString(res.Left.Array, res.Left.Offset, res.Left.Count)
                        Console.WriteLine("{0,7}", k);
                    }
                }

                while (Console.KeyAvailable) Console.ReadKey(true);
                Console.WriteLine("Finished! Press any key to exit.");
                Console.ReadKey(true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("M: {0}", ex);
            }
        }
    }
}
