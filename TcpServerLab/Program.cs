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
                                var data = Encoding.UTF8.GetBytes($"{messages[random.Next(messages.Length)]}");

                                stream.Write(data, 0, data.Length);

                                Thread.Sleep(1000);
                            }
                        });

                // 一直從 Client 收資料
                Task.Run(
                    () =>
                        {
                            // 取得 NetworkStream
                            var stream = client.GetStream();

                            // 宣告資料緩衝區
                            var line = new List<byte[]>();

                            while (true)
                            {
                                // 設定 Buffer 長度
                                var buffer = new byte[10];

                                try
                                {
                                    // 讀取一個 Buffer 長度的資料
                                    var numBytesRead = stream.Read(buffer, 0, buffer.Length);

                                    int newlinePosition;
                                    int numBytesConsumed = 0;

                                    do
                                    {
                                        // 搜尋換行符號的位置
                                        newlinePosition = Array.IndexOf(buffer, (byte)'\n', numBytesConsumed);

                                        if (newlinePosition >= 0)
                                        {
                                            // 將換行符號之間的資料放進緩衝區
                                            line.Add(
                                                buffer.Skip(numBytesConsumed)
                                                    .Take(newlinePosition - numBytesConsumed)
                                                    .ToArray());

                                            // 標記已經處理的資料長度
                                            numBytesConsumed = newlinePosition + 1;

                                            // 緩衝區內的資料已成一包，送給邏輯程序處理。
                                            ProcessData(line.SelectMany(x => x).ToArray());

                                            // 清空緩衝區
                                            line.Clear();
                                        }
                                        else
                                        {
                                            // 將剩餘的資料放進緩衝區
                                            line.Add(
                                                buffer.Skip(numBytesConsumed)
                                                    .Take(numBytesRead - numBytesConsumed)
                                                    .ToArray());
                                        }
                                    }
                                    while (newlinePosition >= 0);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                    break;
                                }
                            }
                        });
            }
        }

        private static void ProcessData(byte[] data)
        {
            Console.WriteLine(Encoding.UTF8.GetString(data));
        }
    }
}