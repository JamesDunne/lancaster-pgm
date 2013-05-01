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
            var config = ProtocolConfiguration.Default;

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
            Console.WriteLine("Press any key to start server...");
            Console.ReadKey(true);

            try
            {
                var server = new Server(config);

                // Server connect always returns:
                await server.Connect(CancellationToken.None);

                const int bufferSize = 9040;
                byte[] buffer = new byte[bufferSize];

                int k = 0;
                for (int j = 0; j < 10; ++j)
                {
                    Console.WriteLine("Sending...");
                    for (int i = 0; i < 100000; ++i, ++k)
                    {
                        Array.Copy(BitConverter.GetBytes(k), 0, buffer, 0, 4);

                        var res = await server.Send(new ArraySegment<byte>(buffer, 0, bufferSize));
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
