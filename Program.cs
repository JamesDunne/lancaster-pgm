using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LANCaster
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!PGM.Detect())
            {
                Console.Error.WriteLine("PGM protocol is required to be installed.");
                Console.Error.WriteLine("To install PGM, open 'Turn Windows features on or off' in Control Panel and enable feature 'Microsoft Message Queue (MSMQ) Server > Server Core > Multicasting Support'.");
                Environment.ExitCode = 1;
                return;
            }

            Task.Run((Func<Task>)Run).Wait();
        }

        static async Task Run()
        {
            var ep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("224.12.19.82"), 0);

            // Run a server-side task:
            Task ts = Task.Run(async () =>
            {
                var server = new Server();

                // Server connect always returns:
                await server.Connect(ep, CancellationToken.None);

                // Send some data so the client can accept:
                await server.Send();

                server.Close();
            });

            // Run a client-side task:
            Task tc = Task.Run(async () =>
            {
                var client = new Client();

                // Client listen needs some data sent to accept:
                var conn = await client.Accept(ep, CancellationToken.None);

                // Read data:
                await conn.Read();
            });

            await Task.WhenAll(ts, tc);
        }
    }
}
