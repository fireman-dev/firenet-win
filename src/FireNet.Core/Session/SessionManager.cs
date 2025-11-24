using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FireNet.Core.Session
{
    public sealed class SessionManager
    {
        private static readonly Lazy<SessionManager> _instance =
            new(() => new SessionManager());

        public static SessionManager Instance => _instance.Value;

        private readonly string _sessionPath;
        private readonly string _crashLogPath;

        // دادهٔ سشن که داخل فایل JSON ذخیره می‌شود
        private class SessionData
        {
            public string? Token { get; set; }
            public string? Username { get; set; }
            public string? DisplayName { get; set; }
        }

        private SessionData _current = new();

        // --------------------------------------------------------------------
        // سازنده‌ی خصوصی (Singleton)
        // --------------------------------------------------------------------
        private SessionManager()
        {
            string appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FireNet");

            if (!Directory.Exists(appDir))
                Directory.CreateDirectory(appDir);

            _sessionPath = Path.Combine(appDir, "session.json");
            _crashLogPath = Path.Combine(appDir, "crash.log");

            LoadSession();
        }

        // --------------------------------------------------------------------
        // بارگذاری سشن از فایل
        // --------------------------------------------------------------------
        private void LoadSession()
        {
            try
            {
                if (File.Exists(_sessionPath))
                {
                    string json = File.ReadAllText(_sessionPath, Encoding.UTF8);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        _current = new SessionData();
                        return;
                    }

                    var data = JsonSerializer.Deserialize<SessionData>(json);

                    if (data == null)
                    {
                        _current = new SessionData();
                        return;
                    }

                    // پاکسازی توکن در لحظه‌ی Load
                    if (!string.IsNullOrWhiteSpace(data.Token))
                        data.Token = SanitizeToken(data.Token);

                    _current = data;
                }
                else
                {
                    _current = new SessionData();
                }
            }
            catch (Exception ex)
            {
                _current = new SessionData();
                LogCrash("LoadSession failed", ex);
            }
        }

        // --------------------------------------------------------------------
        // ذخیره‌کردن سشن در فایل
        // --------------------------------------------------------------------
        private void SaveSession()
        {
            try
            {
                // قبل از ذخیره، توکن اگر هست پاکسازی شود
                if (!string.IsNullOrWhiteSpace(_current.Token))
                    _current.Token = SanitizeToken(_current.Token!);

                string json = JsonSerializer.Serialize(_current, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_sessionPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception ex)
            {
                LogCrash("SaveSession failed", ex);
            }
        }

        // --------------------------------------------------------------------
        // ذخیره توکن
        // --------------------------------------------------------------------
        public void SaveToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _current.Token = null;
                SaveSession();
                return;
            }

            _current.Token = SanitizeToken(token);
            SaveSession();
        }

        // --------------------------------------------------------------------
        // گرفتن توکن
        // --------------------------------------------------------------------
        public string GetToken()
        {
            if (string.IsNullOrWhiteSpace(_current.Token))
                return string.Empty;

            return SanitizeToken(_current.Token!);
        }

        public bool HasToken()
        {
            return !string.IsNullOrWhiteSpace(_current.Token);
        }

        // --------------------------------------------------------------------
        // ذخیره نام کاربر
        // --------------------------------------------------------------------
        public void SaveUser(string username)
        {
            _current.Username = username;
            _current.DisplayName = username;
            SaveSession();
        }

        public string? GetUsername() => _current.Username;

        public string? GetDisplayName() => _current.DisplayName;

        // --------------------------------------------------------------------
        // پاک‌کردن سشن (مثلاً موقع Logout یا Token expired)
        // --------------------------------------------------------------------
        public void ClearSession()
        {
            _current = new SessionData();

            try
            {
                if (File.Exists(_sessionPath))
                    File.Delete(_sessionPath);
            }
            catch (Exception ex)
            {
                LogCrash("ClearSession failed", ex);
            }
        }

        // --------------------------------------------------------------------
        // پاکسازی و نرمال‌سازی توکن
        // --------------------------------------------------------------------
        private static string SanitizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            token = token.Trim();

            var sb = new StringBuilder(token.Length);

            foreach (char c in token)
            {
                // حذف کاراکترهای کنترل (null, newline, tab, etc.)
                if (!char.IsControl(c))
                    sb.Append(c);
            }

            // اگر بعد از پاکسازی خیلی کوتاه بود، عملاً اعتباری ندارد
            return sb.ToString();
        }

        // --------------------------------------------------------------------
        // لاگ کرش‌ها
        // --------------------------------------------------------------------
        public void LogCrash(string message, Exception? ex = null)
        {
            try
            {
                string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{time}] {message}";

                if (ex != null)
                {
                    logEntry += Environment.NewLine + ex + Environment.NewLine;
                }
                else
                {
                    logEntry += Environment.NewLine;
                }

                // محدود نگه داشتن حجم لاگ
                if (File.Exists(_crashLogPath))
                {
                    var lines = File.ReadAllLines(_crashLogPath);
                    if (lines.Length > 2000)
                    {
                        var trimmed = lines[^1000..];
                        File.WriteAllLines(_crashLogPath, trimmed);
                    }
                }

                File.AppendAllText(_crashLogPath, logEntry);
            }
            catch
            {
                // خطا در لاگ گیری نباید باعث کرش بشه
            }
        }
    }
}
