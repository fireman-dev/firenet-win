using System;
using System.Threading;
using System.Threading.Tasks;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;

namespace FireNet.Core.Notifications
{
    public class NotificationService
    {
        private readonly PanelApiClient _api;
        private Timer? _timer;
        private bool _isRunning;

        // هر 60 ثانیه پولینگ انجام شود
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(60);

        public NotificationService(PanelApiClient apiClient)
        {
            _api = apiClient;
        }

        // --------------------------------------------------------------
        // شروع Polling
        // --------------------------------------------------------------
        public void Start()
        {
            if (_isRunning)
                return;

            _timer = new Timer(async _ => await Tick(), null, TimeSpan.Zero, _interval);
            _isRunning = true;
        }

        // --------------------------------------------------------------
        // توقف Polling (مثلاً هنگام Logout)
        // --------------------------------------------------------------
        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            _isRunning = false;
        }

        // --------------------------------------------------------------
        // اجرای هر بار Poll
        // --------------------------------------------------------------
        private async Task Tick()
        {
            try
            {
                NotificationFetchResponse? result = await _api.FetchNotificationsAsync();

                if (result == null || result.Notifications.Count == 0)
                    return;

                foreach (var noti in result.Notifications)
                {
                    ShowToast(noti);
                }
            }
            catch (Exception ex)
            {
                // لاگ‌گیری در صورت نیاز
                Console.WriteLine("Notification error: " + ex.Message);
            }
        }

        // --------------------------------------------------------------
        // نمایش Toast Notification
        // --------------------------------------------------------------
        private void ShowToast(NotificationItem item)
        {
            new ToastContentBuilder()
                .AddText(item.Title)
                .AddText(item.Body)
                .Show();
        }
    }
}
