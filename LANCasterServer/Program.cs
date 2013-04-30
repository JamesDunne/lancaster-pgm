using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LANCaster;

namespace LANCasterServer
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
            // Setup protocol configuration:
            var config = new ProtocolConfiguration(new IPEndPoint(IPAddress.Parse("224.12.19.82"), 1982))
            {
                UsePGM = false,
                UseLoopback = true,
                // Blocking I/O is much faster
                UseNonBlockingIO = true
            };

            // Make sure that PGM is installed if required:
            if (config.UsePGM && !PGM.Detect())
            {
                Console.Error.WriteLine("PGM protocol is required to be installed.");
                Console.Error.WriteLine("To install PGM, open 'Turn Windows features on or off' in Control Panel and enable feature 'Microsoft Message Queue (MSMQ) Server > Server Core > Multicasting Support'.");
                Environment.ExitCode = 1;
                return;
            }

            // Wait for keypress to start:
            while (Console.KeyAvailable) Console.ReadKey(true);
            Console.WriteLine("Press any key to start...");
            Console.ReadKey(true);

            try
            {
                var server = new Server(config);

                // Server connect always returns:
                await server.Connect(CancellationToken.None);

                const int bufferSize = 9040;
                byte[] buffer = new byte[bufferSize];
                int sn = Encoding.ASCII.GetBytes("HELLO", 0, 5, buffer, 0);
                sn = bufferSize;

                for (int j = 0; j < 10; ++j)
                {
                    Console.WriteLine("Wait 3000 ms...");
                    await Task.Delay(3000);

                    Console.WriteLine("Sending...");
                    // Send some data so the client can accept:
                    for (int i = 0; i < 90000; ++i)
                    {
                        var res = await server.Send(new ArraySegment<byte>(buffer, 0, sn));
                        if (res.IsRight)
                        {
                            Console.Error.WriteLine("{0}", res.Right);
                        }
                    }
                }

                server.Close();

                while (Console.KeyAvailable) Console.ReadKey(true);
                Console.WriteLine("Finished! Press any key to exit.");
                Console.ReadKey(true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("{0}", ex);
            }
        }
    }
}
