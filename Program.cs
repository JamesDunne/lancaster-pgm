using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LANCaster
{
    class Program
    {
        static void Main(string[] args)
        {
            //ThreadPool.SetMaxThreads(24, 24);
            //ThreadPool.SetMinThreads(16, 16);

            // Run the main task and wait for its completion:
            Task.Run((Func<Task>)Run).Wait();
        }

        static async Task Run()
        {
            var config = new ProtocolConfiguration(new IPEndPoint(IPAddress.Parse("224.12.19.82"), 1982))
            {
                UsePGM = true,
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

            while (Console.KeyAvailable) Console.ReadKey(true);
            Console.WriteLine("Press any key to start...");
            Console.ReadKey(true);

            try
            {
                // Run a server-side task:
                Task ts = Task.Run(async () =>
                {
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
                            Console.WriteLine("S: Wait 3000 ms...");
                            await Task.Delay(3000);

                            Console.WriteLine("S: Sending...");
                            // Send some data so the client can accept:
                            for (int i = 0; i < 30000; ++i)
                            {
#if true
                                var task0 = server.Send(new ArraySegment<byte>(buffer, 0, sn));
                                var task1 = server.Send(new ArraySegment<byte>(buffer, 0, sn));
                                var task2 = server.Send(new ArraySegment<byte>(buffer, 0, sn));

                                var res0 = await task0;
                                if (res0.IsRight)
                                {
                                    Console.Error.WriteLine("S: {0}", res0.Right);
                                }
                                var res1 = await task1;
                                if (res1.IsRight)
                                {
                                    Console.Error.WriteLine("S: {0}", res1.Right);
                                }
                                var res2 = await task2;
                                if (res2.IsRight)
                                {
                                    Console.Error.WriteLine("S: {0}", res2.Right);
                                }
#else
                                var task = server.Send(new ArraySegment<byte>(buffer, 0, sn));
#endif
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
                        var client = new Client(config);

                        // Client listen needs some data sent to accept:
                        Console.WriteLine("C: Accept...");
                        var conn = await client.Accept(CancellationToken.None);
                        Console.WriteLine("C: Accepted");

                        int recvd = 0;
                        var buf = conn.AllocateBuffer();
                        while (true)
                        {
                            // Read data into our buffer:
                            var res = await conn.Receive(buf);
                            if (res.IsRight)
                            {
                                if (!config.UsePGM)
                                {
                                    if (res.Right == System.Net.Sockets.SocketError.NotConnected) continue;
                                }

                                Console.Error.WriteLine("C: {0}", res.Right);
                                conn.Close();
                                break;
                            }

                            ++recvd;
                            if ((recvd & 511) == 1)
                            {
                                // , Encoding.ASCII.GetString(res.Left.Array, res.Left.Offset, res.Left.Count)
                                Console.WriteLine("C: {0}", recvd);
                            }
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
