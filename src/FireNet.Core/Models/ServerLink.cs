using System;
using System.Text;
using System.Text.Json;
using System.Web;

namespace FireNet.Core.Models
{
    public class ServerLink
    {
        public string Protocol { get; set; }
        public string Tag { get; set; }
        public object Settings { get; set; }
        public object StreamSettings { get; set; }

        // -------------------------------------------------------
        // PARSE VLESS
        // -------------------------------------------------------
        public static ServerLink ParseVless(string link)
        {
            var uri = new Uri(link);

            string uuid = uri.UserInfo;
            string host = uri.Host;
            int port = uri.Port;
            string tag = uri.Fragment.Replace("#", "").Trim();

            var query = HttpUtility.ParseQueryString(uri.Query);

            // Base fields
            string type = query["type"] ?? "tcp";
            string security = query["security"];
            string encryption = query["encryption"] ?? "none";
            string flow = query["flow"];
            string path = query["path"] ?? "/";
            string sni = query["sni"];
            string alpn = query["alpn"];
            string hostHeader = query["host"];
            string serviceName = query["serviceName"];

            // ---------------------------
            // AUTO TLS: real server requirement
            // ---------------------------
            if (string.IsNullOrWhiteSpace(security))
                security = port == 443 ? "tls" : "none";

            // ---------------------------
            // outbound settings (vnext)
            ---------------------------
            var settings = new
            {
                vnext = new[]
                {
                    new {
                        address = host,
                        port = port,
                        users = new[]
                        {
                            new {
                                id = uuid,
                                encryption = encryption,
                                flow = flow
                            }
                        }
                    }
                }
            };

            // ---------------------------
            // TCP FAKE HEADER REQUIRED?
            ---------------------------
            object tcpSettings = null;

            if (type == "tcp")
            {
                // If no TLS and no WS/GRPC → must use fake header (soft97 style)
                bool needFakeHeader =
                    security == "none" &&
                    query["type"] == null &&  // no explicit type
                    query["header"] == null;  // no built-in header

                if (needFakeHeader)
                {
                    tcpSettings = new
                    {
                        header = new
                        {
                            type = "http",
                            request = new
                            {
                                version = "1.1",
                                method = "GET",
                                path = new[] { "/" },
                                headers = new
                                {
                                    Host = new[] { hostHeader ?? host },
                                    Connection = new[] { "keep-alive" },
                                    Pragma = "no-cache",
                                    Accept = new[] { "*/*" },
                                    ["Accept-Encoding"] = new[] { "gzip, deflate" }
                                }
                            }
                        }
                    };
                }
            }

            // ---------------------------
            // streamSettings
            // ---------------------------
            var stream = new
            {
                network = type,
                security = security,

                // TLS
                tlsSettings = security == "tls" ? new
                {
                    allowInsecure = true,
                    serverName = !string.IsNullOrWhiteSpace(sni) ? sni : host,
                    alpn = !string.IsNullOrWhiteSpace(alpn) ? alpn.Split(',') : null
                } : null,

                // WS
                wsSettings = type == "ws" ? new
                {
                    path = path,
                    headers = new
                    {
                        Host = hostHeader ?? host
                    }
                } : null,

                // gRPC
                grpcSettings = type == "grpc" ? new
                {
                    serviceName = serviceName
                } : null,

                // TCP header
                tcpSettings = tcpSettings
            };

            return new ServerLink
            {
                Protocol = "vless",
                Tag = !string.IsNullOrWhiteSpace(tag) ? tag : $"vless-{host}",
                Settings = settings,
                StreamSettings = stream
            };
        }


        // -------------------------------------------------------
        // PARSE VMESS
        // -------------------------------------------------------
        public static ServerLink ParseVmess(string link)
        {
            string base64 = link.Replace("vmess://", "").Trim();
            string json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));

            var vm = JsonSerializer.Deserialize<VmessModel>(json);

            var settings = new
            {
                vnext = new[]
                {
                    new {
                        address = vm.add,
                        port = int.Parse(vm.port),
                        users = new[] {
                            new {
                                id = vm.id,
                                alterId = 0,
                                security = "auto"
                            }
                        }
                    }
                }
            };

            var stream = new
            {
                network = vm.net,
                security = vm.tls != "none" ? "tls" : "none",
                tlsSettings = vm.tls != "none"
                    ? new {
                        serverName = vm.sni,
                        allowInsecure = true
                    }
                    : null,
                wsSettings = vm.net == "ws"
                    ? new {
                        path = vm.path,
                        headers = new { Host = vm.host }
                    }
                    : null
            };

            return new ServerLink
            {
                Protocol = "vmess",
                Tag = vm.ps,
                Settings = settings,
                StreamSettings = stream
            };
        }


        // -------------------------------------------------------
        // PARSE TROJAN
        // -------------------------------------------------------
        public static ServerLink ParseTrojan(string link)
        {
            var uri = new Uri(link);

            string pass = uri.UserInfo;
            string host = uri.Host;
            int port = uri.Port;
            string tag = uri.Fragment.Replace("#", "").Trim();

            var query = HttpUtility.ParseQueryString(uri.Query);

            string security = query["security"] ?? "tls";
            string type = query["type"] ?? "tcp";
            string path = query["path"] ?? "/";
            string hostHeader = query["host"];
            string sni = query["sni"];

            var settings = new
            {
                servers = new[]
                {
                    new {
                        address = host,
                        port = port,
                        password = pass
                    }
                }
            };

            object tcpSettings = null;

            if (type == "tcp")
            {
                tcpSettings = new
                {
                    header = new
                    {
                        type = "http",
                        request = new
                        {
                            version = "1.1",
                            method = "GET",
                            path = new [] { "/" },
                            headers = new {
                                Host = new [] { hostHeader ?? host }
                            }
                        }
                    }
                };
            }

            var stream = new
            {
                network = type,
                security = security,

                tlsSettings = new
                {
                    allowInsecure = true,
                    serverName = sni ?? host
                },

                wsSettings = type == "ws"
                    ? new {
                        path = path,
                        headers = new { Host = hostHeader ?? host }
                    }
                    : null,

                tcpSettings = tcpSettings
            };

            return new ServerLink
            {
                Protocol = "trojan",
                Tag = tag != "" ? tag : $"trojan-{host}",
                Settings = settings,
                StreamSettings = stream
            };
        }


        // -------------------------------------------------------
        // VMESS MODEL
        // -------------------------------------------------------
        private class VmessModel
        {
            public string v { get; set; }
            public string ps { get; set; }
            public string add { get; set; }
            public string port { get; set; }
            public string id { get; set; }
            public string aid { get; set; }
            public string scy { get; set; }
            public string net { get; set; }
            public string type { get; set; }
            public string host { get; set; }
            public string path { get; set; }
            public string tls { get; set; }
            public string sni { get; set; }
        }
    }
}
