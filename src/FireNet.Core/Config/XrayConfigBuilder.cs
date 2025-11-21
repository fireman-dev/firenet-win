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

        private const string SocksHost = "127.0.0.1";
        private const int SocksPort = 10808;

        public XrayConfigBuilder()
        {
            _configRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FireNet",
                "xray",
                "configs");

            if (!Directory.Exists(_configRoot))
                Directory.CreateDirectory(_configRoot);
        }

        // -------------------------------------------------
        // BuildConfig: links → server objects → json file
        // -------------------------------------------------
        public string BuildConfig(List<string> links)
        {
            if (links == null || links.Count == 0)
                throw new ArgumentException("No links provided", nameof(links));

            var parsedLinks = new List<ServerLink>();

            foreach (var link in links)
            {
                if (string.IsNullOrWhiteSpace(link))
                    continue;

                parsedLinks.Add(ParseLink(link));
            }

            if (parsedLinks.Count == 0)
                throw new Exception("No valid links found");

            var configObject = new
            {
                log = new { loglevel = "warning" },

                inbounds = CreateInbounds(),

                outbounds = CreateOutbounds(parsedLinks),

                routing = CreateRouting(parsedLinks),

                dns = CreateDns()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(configObject, options);

            string filePath = Path.Combine(_configRoot, $"{Guid.NewGuid()}.json");
            File.WriteAllText(filePath, json);

            return filePath;
        }

        // -------------------------------------------------
        // Parse a single link
        // -------------------------------------------------
        private ServerLink ParseLink(string link)
        {
            if (link.StartsWith("vless://", StringComparison.OrdinalIgnoreCase))
                return ServerLink.ParseVless(link);

            if (link.StartsWith("vmess://", StringComparison.OrdinalIgnoreCase))
                return ServerLink.ParseVmess(link);

            if (link.StartsWith("trojan://", StringComparison.OrdinalIgnoreCase))
                return ServerLink.ParseTrojan(link);

            throw new Exception("Unsupported link type");
        }

        // -------------------------------------------------
        // Inbounds (SOCKS for system proxy)
        // -------------------------------------------------
        private object[] CreateInbounds()
        {
            return new object[]
            {
                new
                {
                    tag = "socks-in",
                    port = SocksPort,
                    listen = SocksHost,
                    protocol = "socks",
                    sniffing = new
                    {
                        enabled = true,
                        destOverride = new[] { "http", "tls" },
                        routeOnly = false
                    },
                    settings = new
                    {
                        auth = "noauth",
                        udp = true,
                        ip = SocksHost,
                        allowTransparent = false
                    }
                }
            };
        }

        // -------------------------------------------------
        // Outbounds (servers + direct + blocked)
        // -------------------------------------------------
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
                    streamSettings = s.StreamSettings,
                    mux = new
                    {
                        enabled = false,
                        concurrency = -1
                    }
                });
            }

            // direct
            result.Add(new
            {
                tag = "direct",
                protocol = "freedom",
                settings = new { }
            });

            // blocked
            result.Add(new
            {
                tag = "blocked",
                protocol = "blackhole",
                settings = new { }
            });

            return result;
        }

        // -------------------------------------------------
        // Routing (ads → blocked, private/IR → direct, rest → first server)
        // -------------------------------------------------
        private object CreateRouting(List<ServerLink> servers)
        {
            var rules = new List<object>();

            // Ads → blocked
            rules.Add(new
            {
                type = "field",
                domain = new[] { "geosite:category-ads" },
                outboundTag = "blocked"
            });

            // Private domains → direct
            rules.Add(new
            {
                type = "field",
                domain = new[] { "geosite:private" },
                outboundTag = "direct"
            });

            // Private IPs → direct
            rules.Add(new
            {
                type = "field",
                ip = new[] { "geoip:private" },
                outboundTag = "direct"
            });

            // IR domains → direct (if lists exist)
            rules.Add(new
            {
                type = "field",
                domain = new[] { "geosite:category-ir", "domain:ir" },
                outboundTag = "direct"
            });

            // IR IPs → direct
            rules.Add(new
            {
                type = "field",
                ip = new[] { "geoip:ir" },
                outboundTag = "direct"
            });

            // All other traffic → first server
            if (servers.Count > 0)
            {
                rules.Add(new
                {
                    type = "field",
                    port = "0-65535",
                    outboundTag = servers[0].Tag
                });
            }

            return new
            {
                domainStrategy = "IPIfNonMatch",
                rules = rules
            };
        }

        // -------------------------------------------------
        // DNS
        // -------------------------------------------------
        private object CreateDns()
        {
            return new
            {
                hosts = new
                {
                    // dns_google = "8.8.8.8"
                },
                servers = new object[]
                {
                    "1.1.1.1",
                    "8.8.8.8"
                }
            };
        }
    }
}
