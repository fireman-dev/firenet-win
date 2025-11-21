using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FireNet.Core.Xray
{
    /// <summary>
    /// مدیریت پروکسی سیستم (WinINet) برای یوزر فعلی
    /// socks5 روی 127.0.0.1:10808
    /// </summary>
    public static class SystemProxyManager
    {
        private const string InternetSettingsKey =
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(
            IntPtr hInternet,
            int dwOption,
            IntPtr lpBuffer,
            int dwBufferLength);

        public static void EnableSocksProxy(string host, int port)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(InternetSettingsKey))
            {
                if (key == null)
                    return;

                // فعال کردن پروکسی
                key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                // فقط socks روی پورت 10808
                key.SetValue("ProxyServer", $"socks={host}:{port}", RegistryValueKind.String);
                // محلی‌ها بدون پروکسی
                key.SetValue("ProxyOverride", "localhost;127.0.0.1;<local>", RegistryValueKind.String);
            }

            ApplyInternetSettings();
        }

        public static void DisableProxy()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(InternetSettingsKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(InternetSettingsKey))
            {
                if (key == null)
                    return;

                // فقط غیر فعال شدن کلی
                key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
            }

            ApplyInternetSettings();
        }

        private static void ApplyInternetSettings()
        {
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
