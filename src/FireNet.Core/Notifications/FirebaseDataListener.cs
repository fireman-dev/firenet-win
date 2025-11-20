using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FireNet.Core.Notifications
{
    public class FirebaseDataListener
    {
        private readonly string _fcmToken;
        private readonly string _firebaseServerUrl;
        private readonly CancellationTokenSource _cts = new();
        private readonly HttpClient _http = new HttpClient();

        // Eventها برای خروجی
        public event Action<string> OnForceLogout;
        public event Action<string> OnShowMessage;
        public event Action OnRefreshRequested;
        public event Action OnUpdateRequired;

        public FirebaseDataListener(string fcmToken, string firebaseServerUrl)
        {
            _fcmToken = fcmToken;
            _firebaseServerUrl = firebaseServerUrl.TrimEnd('/');
        }

        // -----------------------------------------------------
        // شروع Listener (Thread background)
        // -----------------------------------------------------
        public void Start()
        {
            Task.Run(() => ListenLoop(_cts.Token));
        }

        // -----------------------------------------------------
        // توقف Listener
        // -----------------------------------------------------
        public void Stop()
        {
            _cts.Cancel();
        }

        // -----------------------------------------------------
        // حلقه پایدار دریافت نوتیفیکیشن
        // -----------------------------------------------------
        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // فیک URL برای مثال Long Polling
                    var url = $"{_firebaseServerUrl}/listen?token={_fcmToken}";

                    var response = await _http.GetAsync(url, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        await Task.Delay(2000, ct);
                        continue;
                    }

                    string raw = await response.Content.ReadAsStringAsync(ct);

                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    HandleIncomingMessage(raw);
                }
                catch
                {
                    await Task.Delay(2000, ct);
                }
            }
        }

        // -----------------------------------------------------
        // پردازش پیام Data Message
        // -----------------------------------------------------
        private void HandleIncomingMessage(string json)
        {
            try
            {
                var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("action", out JsonElement actionEl))
                    return;

                string action = actionEl.GetString();

                switch (action)
                {
                    case "force_logout":
                        OnForceLogout?.Invoke("Logout requested by server");
                        break;

                    case "show_message":
                        if (doc.RootElement.TryGetProperty("message", out JsonElement msg))
                            OnShowMessage?.Invoke(msg.GetString());
                        break;

                    case "refresh":
                        OnRefreshRequested?.Invoke();
                        break;

                    case "update_required":
                        OnUpdateRequired?.Invoke();
                        break;
                }
            }
            catch
            {
                // ignore bad payload
            }
        }
    }
}
