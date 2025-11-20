using System;
using System.IO;
using System.Text.Json;

namespace FireNet.Core.Session
{
    public class SessionManager
    {
        private readonly string _sessionPath;

        private class SessionFileModel
        {
            public string token { get; set; }
            public long lastLogin { get; set; }
        }

        public SessionManager()
        {
            _sessionPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FireNet",
                "session.json"
            );

            string folder = Path.GetDirectoryName(_sessionPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        // -----------------------------------------------------
        // ذخیره توکن JWT
        // -----------------------------------------------------
        public void SaveToken(string token)
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

        // -----------------------------------------------------
        // گرفتن توکن فعلی
        // -----------------------------------------------------
        public string GetToken()
        {
            if (!File.Exists(_sessionPath))
                return null;

            try
            {
                string json = File.ReadAllText(_sessionPath);
                var obj = JsonSerializer.Deserialize<SessionFileModel>(json);
                return obj.token;
            }
            catch
            {
                return null;
            }
        }

        // -----------------------------------------------------
        // کاربر لاگین است؟
        // -----------------------------------------------------
        public bool IsLoggedIn()
        {
            string token = GetToken();
            return !string.IsNullOrEmpty(token);
        }

        // -----------------------------------------------------
        // پاک کردن نشست
        // -----------------------------------------------------
        public void ClearSession()
        {
            if (File.Exists(_sessionPath))
                File.Delete(_sessionPath);
        }

        // -----------------------------------------------------
        // زمان آخرین لاگین برای تمدید نشست ۴۸ ساعته
        // (اگر خواستی ازش استفاده کنی)
        // -----------------------------------------------------
        public long GetLastLoginTime()
        {
            if (!File.Exists(_sessionPath))
                return 0;

            try
            {
                string json = File.ReadAllText(_sessionPath);
                var obj = JsonSerializer.Deserialize<SessionFileModel>(json);
                return obj.lastLogin;
            }
            catch
            {
                return 0;
            }
        }
    }
}
