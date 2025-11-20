using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FireNet.Core.Models;

namespace FireNet.Core.Config
{
    public class XrayConfigBuilder
    {
        private readonly string _configRoot;

        public XrayConfigBuilder()
        {
            // مسیر ذخیره فایل‌های کانفیگ
            _configRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FireNet",
                "xray",
                "configs");

            if (!Directory.Exists(_configRoot))
                Directory.CreateDirectory(_configRoot);
        }

        // -----------------------------
        // متد اصلی ساخت فایل کانفیگ
        // -----------------------------
        public string BuildConfig(List<string> links)
        {
            var parsedLinks = new List<ServerLink>();

            foreach (var link in links)
                parsedLinks.Add(ParseLink(link));

            var configObject = new
            {
                log = new { loglevel = "warning" },
                inbounds = CreateInbounds(),
                outbounds = CreateOutbounds(parsedLinks),
                routing = CreateRouting(parsedLinks),
                dns = CreateDns()
            };

            string json = JsonSerializer.Serialize(configObject, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string filePath = Path.Combine(_configRoot, $"{Guid.NewGuid()}.json");
            File.WriteAllText(filePath, json);

            return filePath;
        }

        // -----------------------------
        // پارس لینک‌ها
        // -----------------------------
        private ServerLink ParseLink(string link)
        {
            if (link.StartsWith("vless://"))
                return ServerLink.ParseVless(link);

            if (link.StartsWith("vmess://"))
                return ServerLink.ParseVmess(link);

            if (link.StartsWith("trojan://"))
                return ServerLink.ParseTrojan(link);

            throw new Exception("Unsupported link type");
        }

        // -----------------------------
        // inbounds
        // -----------------------------
        private object[] CreateInbounds()
        {
            return new object[]
            {
                new
                {
                    tag = "socks-in",
                    protocol = "socks",
                    listen = "127.0.0.1",
                    port = 10808,
                    settings = new { udp = true },
                    sniffing = new
                    {
                        enabled = true,
                        destOverride = new [] {"http", "tls"}
                    }
                }
            };
        }

        // -----------------------------
        // outbounds
        // -----------------------------
        private List<object> CreateOutbounds(List<ServerLink> servers)
        {
            var result = new List<object>();

            foreach (var s in servers)
            {
                result.Add(new
                {
                    tag = s.Tag,
                    protocol = s.Protocol,
                    settings = s.Settings,
                    streamSettings = s.StreamSettings
                });
            }

            // outbound مستقیم
            result.Add(new
            {
                tag = "direct",
                protocol = "freedom",
                settings = new { }
            });

            // outbound بلاک
            result.Add(new
            {
                tag = "blocked",
                protocol = "blackhole",
                settings = new { }
            });

            return result;
        }

        // -----------------------------
        // routing
        // -----------------------------
        private object CreateRouting(List<ServerLink> servers)
        {
            var rules = new List<object>();

            // بلاک تبلیغات
            rules.Add(new
            {
                type = "field",
                domain = new[] { "geosite:category-ads" },
                outboundTag = "blocked"
            });

            // شبکه داخلی → direct
            rules.Add(new
            {
                type = "field",
                ip = new[] { "geoip:private" },
                outboundTag = "direct"
            });

            // باقی ترافیک → اولین سرور
            if (servers.Count > 0)
            {
                rules.Add(new
                {
                    type = "field",
                    outboundTag = servers[0].Tag
                });
            }

            return new
            {
                domainStrategy = "AsIs",
                rules = rules
            };
        }

        // -----------------------------
        // dns
        // -----------------------------
        private object CreateDns()
        {
            return new
            {
                servers = new object[]
                {
                    "1.1.1.1",
                    "8.8.8.8"
                }
            };
        }
    }
}
