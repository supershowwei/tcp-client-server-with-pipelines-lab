using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpServerLab
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 2611);

            server.Start();

            while (true)
            {
                var client = server.AcceptTcpClient();

                // 一直送資料給 Client
                Task.Run(
                    () =>
                        {
                            var stream = client.GetStream();
                            var random = new Random(Guid.NewGuid().GetHashCode());

                            while (true)
                            {
                                var data = Encoding.UTF8.GetBytes(random.Next(int.MaxValue).ToString());

                                stream.Write(data, 0, data.Length);

                                Thread.Sleep(5000);
                            }
                        });

                // 一直從 Client 收資料
                Task.Run(
                    () =>
                        {
                            var stream = client.GetStream();
                            var buffer = new byte[client.ReceiveBufferSize];

                            while (true)
                            {
                                if (stream.CanRead)
                                {
                                    var content = new List<byte[]>();

                                    do
                                    {
                                        var numBytesRead = stream.Read(buffer, 0, buffer.Length);

                                        content.Add(buffer.Take(numBytesRead).ToArray());
                                    }
                                    while (stream.DataAvailable);

                                    Console.WriteLine(Encoding.UTF8.GetString(content.SelectMany(x => x).ToArray()));
                                }
                            }
                        });
            }
        }
    }
}