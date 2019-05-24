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
                        var messages = new[]
                                       {
                                           "abc\nde\nfgh\n",
                                           "VCnDanM7\n",
                                           "nDQVyYUb\n",
                                           "Cp2y5Yyz\n",
                                           "W3vNzsMR\n",
                                           "VUFX96Xu\n",
                                           "rMcwU5ZgmyqmkSs4\n",
                                           "hGh6mx3xn8skUNAD\n",
                                           "KCHrnKD6YWfSvD6s\n",
                                           "wGntfXhMv9yGAQbd\n",
                                           "PpWHVQhNYvmDwWvm\n",
                                           "vnPeS8hmZvCQy3tdKXebpSYC5ykPfKXD\n",
                                           "awuSMngVaWZ8GP49ZtGpks34apxZuKUZ\n",
                                           "SBeA9c4ACnPqkNqKDwyG4wmrHa2dxhvS\n",
                                           "rUS5K3vYB9n5YefNXyqBbgs7FHTzVfH2\n",
                                           "Bv2Wh5z5MUPXAHUAmtUhqrcTH9Cr8Ze5\n",
                                           "6WN8srbdnemUtYeMm2ypDAXpvAwmfgRphQfQ8fgqkKBUprZp57CXW99spNfNfm49\n",
                                           "FukygNhW6Fr2bCgCeeuUqQWvPRvbCYvYgwdyV6sfKzh3wVBAfUsCUqs6aC2TSEV7\n",
                                           "2aMXUZ6w39DvtstyHRPVS2AAMQRMQ5QQBvSNs36m4FvH982c3g3vkhHXYYcEA34h\n",
                                           "uyP2VQedUnbaScmQkracBDhtCSeSxQU4zRYP856qpTq5A6pddPHpRDy9GNTPywtp\n",
                                           "GC6KRMdhzEp6grFbhSFqyNybm9gnYAZAWSc4vzMT9eNtFb4Ysx2xYaTExb7a6hEs\n"
                                       };

                        var stream = client.GetStream();
                        var clientId = Guid.NewGuid();
                        var random = new Random(clientId.GetHashCode());

                        while (true)
                        {
                            //var data = Encoding.UTF8.GetBytes($"{clientId}: {random.Next(int.MaxValue).ToString()}");
                            var data = Encoding.UTF8.GetBytes($"{messages[random.Next(messages.Length)]}");

                            stream.Write(data, 0, data.Length);

                            Thread.Sleep(1000);
                        }
                    });

            Console.ReadKey();
            return;

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

                                var buffer = result.Buffer;

                                SequencePosition? position;

                                do
                                {
                                    position = buffer.PositionOf((byte)'\n');

                                    if (position == null) continue;

                                    var line = buffer.Slice(0, position.Value);

                                    foreach (var segment in line)
                                    {
                                        Console.WriteLine(Encoding.UTF8.GetString(segment.ToArray()));
                                    }

                                    var next = buffer.GetPosition(1, position.Value);

                                    buffer = buffer.Slice(next);
                                }
                                while (position != null);

                                //reader.AdvanceTo(buffer.End);
                                reader.AdvanceTo(buffer.Start, buffer.End);
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

            while (true)
            {
                try
                {
                    var buffer = new byte[client.ReceiveBufferSize];

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