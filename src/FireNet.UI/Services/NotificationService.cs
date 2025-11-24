using System;
using System.Threading;
using System.Threading.Tasks;
using FireNet.Core.Api;
using FireNet.Core.Api.Dto.Notifications;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

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

            // WinAppSDK Toast Init
            AppNotificationManager.Default.Register();
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
                var response = await _api.FetchNotificationsAsync();

                if (response == null || response.Notifications.Count == 0)
                    return;

                foreach (var noti in response.Notifications)
                {
                    ShowToast(noti);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Notification Error: " + ex.Message);
            }
        }

        // نسخه پایدار WinAppSDK
        private void ShowToast(NotificationItem item)
        {
            var builder = new AppNotificationBuilder()
                .AddText(item.Title)
                .AddText(item.Body);

            var notification = builder.BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
    }
}
