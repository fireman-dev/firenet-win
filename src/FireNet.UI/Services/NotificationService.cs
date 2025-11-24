using System;
using System.Diagnostics;
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

            _timer = new Timer(async _ => await Tick(), null, TimeSpan.Zero, _interval);
            _isRunning = true;
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            _isRunning = false;
        }

        private async Task Tick()
        {
            try
            {
                var result = await _api.FetchNotificationsAsync();

                if (result == null || result.Notifications.Count == 0)
                    return;

                foreach (var n in result.Notifications)
                {
                    ShowToast(n.Title, n.Body);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Notification Error: " + ex.Message);
            }
        }

        // -----------------------------------------------------------
        // ✔ Toast Notification با PowerShell (سازگار با .NET 9 + GitHub Actions)
        // -----------------------------------------------------------
        private void ShowToast(NotificationItem item)
        {
            new ToastContentBuilder()
                .AddText(item.Title)
                .AddText(item.Body)
                .Show();    // ← نسخه 7.1.3 این را پشتیبانی می‌کند
        }

        private string Escape(string s)
        {
            return s.Replace("'", "''").Replace("\"", "\\\"");
        }
    }
}
