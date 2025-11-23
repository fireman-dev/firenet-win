using System;
using System.IO;
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

        private class SessionData
        {
            public string? Token { get; set; }
            public string? Username { get; set; }
            public string? DisplayName { get; set; }
        }

        private SessionData _current = new();

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

        private void LoadSession()
        {
            try
            {
                if (File.Exists(_sessionPath))
                {
                    string json = File.ReadAllText(_sessionPath);
                    var data = JsonSerializer.Deserialize<SessionData>(json);
                    if (data != null)
                        _current = data;
                }
            }
            catch
            {
                _current = new SessionData();
            }
        }

        private void SaveSession()
        {
            try
            {
                string json = JsonSerializer.Serialize(_current, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_sessionPath, json);
            }
            catch
            {
                // ignore
            }
        }

        public void SaveToken(string token)
        {
            _current.Token = token;
            SaveSession();
        }

        public void SaveUser(string username)
        {
            _current.Username = username;
            _current.DisplayName = username;
            SaveSession();
        }

        public string GetToken()
        {
            return _current.Token ?? string.Empty;
        }

        public bool HasToken()
        {
            return !string.IsNullOrWhiteSpace(_current.Token);
        }

        public void ClearSession()
        {
            _current = new SessionData();
            try
            {
                if (File.Exists(_sessionPath))
                    File.Delete(_sessionPath);
            }
            catch
            {
                // ignore
            }
        }

        // برای لاگ کرش‌ها
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
