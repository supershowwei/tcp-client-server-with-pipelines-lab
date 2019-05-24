using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TcpClientLab
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var client = new TcpClient();

            client.Connect(IPAddress.Parse("127.0.0.1"), 2611);

            // 一直往 Server 送資料
            Task.Run(
                () =>
                    {
                        var stream = client.GetStream();
                        var clientId = Guid.NewGuid();
                        var random = new Random(clientId.GetHashCode());

                        while (true)
                        {
                            var data = Encoding.UTF8.GetBytes($"{clientId}: {random.Next(int.MaxValue).ToString()}");

                            stream.Write(data, 0, data.Length);

                            Thread.Sleep(1000);
                        }
                    });

            // 一直從 Server 接收資料
            var pipe = new Pipe();
            var writer = pipe.Writer;
            var reader = pipe.Reader;

            Task.Run(async () => await NormalWrite(writer, client));
            //Task.Run(async () => await AnotherWrite(writer, client));

            Task.Run(
                async () =>
                    {
                        while (true)
                        {
                            try
                            {
                                var result = await reader.ReadAsync();

                                if (result.IsCompleted) break;

                                foreach (var segment in result.Buffer)
                                {
                                    Console.WriteLine(Encoding.UTF8.GetString(segment.ToArray()));
                                }

                                reader.AdvanceTo(result.Buffer.End);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                        }

                        reader.Complete();
                    });

            Console.ReadKey();
        }

        private static async Task NormalWrite(PipeWriter writer, TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[client.ReceiveBufferSize];

            while (true)
            {
                try
                {
                    var numBytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (numBytesRead == 0) continue;

                    await writer.WriteAsync(new ReadOnlyMemory<byte>(buffer.Take(numBytesRead).ToArray()));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
            }

            writer.Complete();
        }

        private static async Task AnotherWrite(PipeWriter writer, TcpClient client)
        {
            while (true)
            {
                try
                {
                    var buffer = writer.GetMemory(1024);
                    MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment);

                    var numBytesRead = await client.Client.ReceiveAsync(segment, SocketFlags.None);

                    if (numBytesRead == 0) continue;

                    writer.Advance(numBytesRead);

                    var flushResult = await writer.FlushAsync();

                    if (flushResult.IsCompleted) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    break;
                }
            }

            writer.Complete();
        }
    }
}