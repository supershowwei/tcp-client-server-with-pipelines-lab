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
                            return;

                            var stream = client.GetStream();
                            var random = new Random(Guid.NewGuid().GetHashCode());

                            var messages = new string[] { "123\n456", "\n789\n" };
                            var index = 0;

                            while (true)
                            {
                                if (index == messages.Length) index = 0;

                                //var data = Encoding.UTF8.GetBytes(random.Next(int.MaxValue).ToString());
                                var data = Encoding.UTF8.GetBytes(messages[index++]);

                                stream.Write(data, 0, data.Length);

                                Thread.Sleep(30000);
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
                        });
            }
        }

        private static void ProcessData(byte[] data)
        {
            Console.WriteLine(Encoding.UTF8.GetString(data));
        }
    }
}