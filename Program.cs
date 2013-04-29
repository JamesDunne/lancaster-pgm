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
            // Make sure that PGM is installed:
            if (!PGM.Detect())
            {
                Console.Error.WriteLine("PGM protocol is required to be installed.");
                Console.Error.WriteLine("To install PGM, open 'Turn Windows features on or off' in Control Panel and enable feature 'Microsoft Message Queue (MSMQ) Server > Server Core > Multicasting Support'.");
                Environment.ExitCode = 1;
                return;
            }

            // Run the main task and wait for its completion:
            Task.Run((Func<Task>)Run).Wait();
        }

        static async Task Run()
        {
            try
            {
                var ep = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("224.12.19.82"), 0);

                // Run a server-side task:
                Task ts = Task.Run(async () =>
                {
                    try
                    {
                        var server = new Server();

                        // Server connect always returns:
                        await server.Connect(ep, CancellationToken.None);

                        // Send some data so the client can accept:
                        for (int i = 0; i < 60000; ++i)
                        {
                            var res = await server.Send();
                            if (res.IsRight)
                            {
                                Console.Error.WriteLine("S: {0}", res.Right);
                            }
                        }

                        Console.WriteLine("S: Done");
                        server.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("S: {0}", ex);
                    }
                });

                // Run a client-side task:
                Task tc = Task.Run(async () =>
                {
                    try
                    {
                        var client = new Client();

                        // Client listen needs some data sent to accept:
                        var conn = await client.Accept(ep, CancellationToken.None);

                        int recvd = 0;
                        var buf = conn.AllocateBuffer();
                        while (true)
                        {
                            // Read data into our buffer:
                            var res = await conn.Receive(buf);
                            if (res.IsRight)
                            {
                                Console.Error.WriteLine("C: {0}", res.Right);
                                conn.Close();
                                break;
                            }

                            ++recvd;
                            //Console.WriteLine("C: {0} {1}", Encoding.ASCII.GetString(res.Left.Array, res.Left.Offset, res.Left.Count), recvd);
                        }

                        Console.WriteLine("C: {0}", recvd);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                });

                // Wait for both tasks:
                await Task.WhenAll(ts, tc);

                Console.WriteLine("Finished!");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("M: {0}", ex);
            }
        }
    }
}
