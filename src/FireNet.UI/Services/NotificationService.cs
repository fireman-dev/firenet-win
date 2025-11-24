using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto.Notifications;

namespace FireNet.UI.Services
{
    public class NotificationService
    {
        private readonly PanelApiClient _api;
        private Timer? _timer;
        private bool _isRunning;

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        public NotificationService(PanelApiClient api)
        {
            _api = api;
        }

        public void Start()
        {
            if (_isRunning)
                return;

            Log("NotificationService.Start()");
            _timer = new Timer(async _ => await Tick(), null, TimeSpan.Zero, _interval);
            _isRunning = true;
        }

        public void Stop()
        {
            Log("NotificationService.Stop()");
            _timer?.Dispose();
            _timer = null;
            _isRunning = false;
        }

        private async Task Tick()
        {
            try
            {
                Log("Tick() started");

                var result = await _api.FetchNotificationsAsync();

                if (result == null)
                {
                    Log("result == null");
                    return;
                }

                if (result.Notifications == null)
                {
                    Log("result.Notifications == null");
                    return;
                }

                Log($"Received notifications count = {result.Notifications.Count}");

                if (result.Notifications.Count == 0)
                    return;

                foreach (var n in result.Notifications)
                {
                    if (n == null)
                    {
                        Log("Notification item == null");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(n.Title) && string.IsNullOrWhiteSpace(n.Body))
                    {
                        Log("Notification title/body empty");
                    }

                    ShowToast(n.Title ?? "", n.Body ?? "");

                    Log($"Toast shown: {n.Title}");
                }
            }
            catch (Exception ex)
            {
                Log("Tick() ERROR: " + ex.ToString());
            }
        }

        // ←← Toast via PowerShell (سازگار با WPF + .NET 8 + GitHub Actions)
        private void ShowToast(string title, string message)
        {
            try
            {
                Log($"ShowToast called title='{title}' message='{message}'");

                string script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null

$Template = [Windows.UI.Notifications.ToastTemplateType]::ToastText02
$XML = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent($Template)

$TextNodes = $XML.GetElementsByTagName('text')
$TextNodes.Item(0).AppendChild($XML.CreateTextNode('{Escape(title)}')) > $null
$TextNodes.Item(1).AppendChild($XML.CreateTextNode('{Escape(message)}')) > $null

$Toast = [Windows.UI.Notifications.ToastNotification]::new($XML)
$Notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('FireNet')
$Notifier.Show($Toast)
";

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                Log("ShowToast completed");
            }
            catch (Exception ex)
            {
                Log("ShowToast ERROR: " + ex.ToString());
            }
        }

        private string Escape(string s)
        {
            return s.Replace("'", "''").Replace("\"", "\\\"");
        }

        // -------------------------------------------------------------------
        // LOGGING — با همان ساختاری که HomeViewModel دارد اما مخصوص این سرویس
        // -------------------------------------------------------------------
        private void Log(string msg)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, "logs");
                string path = Path.Combine(dir, "notifications.log");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }
    }
}
