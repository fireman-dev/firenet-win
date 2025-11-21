using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;

namespace FireNet.Core.Xray
{
    public static class RealDelayTester
    {
        // Main entry
        public static async Task<long> MeasureAsync(string link)
        {
            try
            {
                var info = ParseServer(link);
                if (info == null)
                    return -1;

                return await TcpDelay(info.Value.host, info.Value.port);
            }
            catch
            {
                return -1;
            }
        }

        // ---------------------------
        // TCP handshake timing
        // ---------------------------
        private static async Task<long> TcpDelay(string host, int port)
        {
            try
            {
                var sw = new Stopwatch();
                using var client = new TcpClient
                {
                    ReceiveTimeout = 2500,
                    SendTimeout = 2500
                };

                sw.Start();
                var t = client.ConnectAsync(host, port);

                var completed = await Task.WhenAny(t, Task.Delay(2500));
                if (completed != t)
                    return -1;

                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch
            {
                return -1;
            }
        }

        // ---------------------------
        // Parse links → host + port
        // ---------------------------
        private static (string host, int port)? ParseServer(string link)
        {
            try
            {
                if (link.StartsWith("vless://") ||
                    link.StartsWith("trojan://"))
                {
                    var uri = new Uri(link);
                    return (uri.Host, uri.Port);
                }

                if (link.StartsWith("vmess://"))
                {
                    string base64 = link.Replace("vmess://", "").Trim();
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));

                    var vm = System.Text.Json.JsonSerializer.Deserialize<Vmess>(json);
                    return (vm.add, int.Parse(vm.port));
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private class Vmess
        {
            public string add { get; set; }
            public string port { get; set; }
        }
    }
}
