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

        private class SessionFileModel
        {
            public string token { get; set; }
            public long lastLogin { get; set; }
        }

        private SessionManager()
        {
            string baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FireNet"
            );

            _sessionPath = Path.Combine(baseFolder, "session.json");
            _crashLogPath = Path.Combine(baseFolder, "crash.log");

            try
            {
                if (!Directory.Exists(baseFolder))
                    Directory.CreateDirectory(baseFolder);
            }
            catch (Exception ex)
            {
                LogCrash("Failed creating session folder", ex);
            }
        }

        // ----------------------------
        // Save JWT token
        // ----------------------------
        public void SaveToken(string token)
        {
            try
            {
                var model = new SessionFileModel
                {
                    token = token,
                    lastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                string json = JsonSerializer.Serialize(model, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_sessionPath, json);
            }
            catch (Exception ex)
            {
                LogCrash("SaveToken error", ex);
            }
        }

        // ----------------------------
        // Read token
        // ----------------------------
        public string GetToken()
        {
            try
            {
                if (!File.Exists(_sessionPath))
                    return null;

                string json = File.ReadAllText(_sessionPath);
                var obj = JsonSerializer.Deserialize<SessionFileModel>(json);

                return obj?.token;
            }
            catch (Exception ex)
            {
                LogCrash("GetToken error", ex);
                return null;
            }
        }

        // ----------------------------
        // Property → user is logged in?
        // ----------------------------
        public bool IsLoggedIn => !string.IsNullOrEmpty(GetToken());

        // ----------------------------
        // Clear session
        // ----------------------------
        public void ClearSession()
        {
            try
            {
                if (File.Exists(_sessionPath))
                    File.Delete(_sessionPath);
            }
            catch (Exception ex)
            {
                LogCrash("ClearSession error", ex);
            }
        }

        // ----------------------------
        // Last login time
        // ----------------------------
        public long GetLastLoginTime()
        {
            try
            {
                if (!File.Exists(_sessionPath))
                    return 0;

                string json = File.ReadAllText(_sessionPath);
                var obj = JsonSerializer.Deserialize<SessionFileModel>(json);
                return obj?.lastLogin ?? 0;
            }
            catch (Exception ex)
            {
                LogCrash("GetLastLoginTime error", ex);
                return 0;
            }
        }

        // ----------------------------
        // Crash logging
        // ----------------------------
        public void LogCrash(string title, Exception ex)
        {
            try
            {
                string log = $"[{DateTime.Now}] {title}\n{ex}\n-------------------------\n";
                File.AppendAllText(_crashLogPath, log);
            }
            catch
            {
                // نمی‌ذاریم کرش جدید تولید بشه
            }
        }
    }
}
