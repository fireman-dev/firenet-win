using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FireNet.Core.Api.Dto;
using FireNet.Core.Api.Dto.Notifications;
using FireNet.Core.Session;

namespace FireNet.Core.Api
{
    public class PanelApiClient
    {
        private readonly HttpClient _http;
        private readonly SessionManager _session;
        private readonly string _baseUrl;

        public PanelApiClient(SessionManager sessionManager, string baseUrl)
        {
            _session = sessionManager;
            _baseUrl = baseUrl.TrimEnd('/');

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(25)
            };
        }

        // -----------------------------------------------------------
        // اضافه کردن توکن JWT به هدر
        // -----------------------------------------------------------
        private void AttachToken()
        {
            // همیشه قبل از هر چیز، Authorization پاک شود
            _http.DefaultRequestHeaders.Authorization = null;

            string token = _session.GetToken();

            if (string.IsNullOrWhiteSpace(token))
                return; // توکنی نیست → نباید Authorization ست شود

            // پاکسازی کامل کاراکترهای خراب، newline، null، control-char
            token = SanitizeToken(token);

            // اگر طول کمتر از 20 بود، یعنی توکن واقعی نیست → ازش استفاده نکن
            if (token.Length < 20)
                return;

            // در این نقطه مطمئنیم توکن سالمه → هدر تنظیم می‌شود
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        private static string SanitizeToken(string token)
        {
            token = token.Trim();

            var sb = new StringBuilder(token.Length);

            foreach (char c in token)
            {
                // حذف کامل همه کاراکترهای کنترل (مثل \0، \n، \r، BOM)
                if (!char.IsControl(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        // -----------------------------------------------------------
        // هندل کردن 401 (توکن نامعتبر یا منقضی)
        // -----------------------------------------------------------
        private void HandleUnauthorized(string body)
        {
            _session.ClearSession();
            throw new Exception("Token expired");
        }

        // -----------------------------------------------------------
        // LOGIN
        // -----------------------------------------------------------
        public async Task<LoginResponse> LoginAsync(LoginRequest req)
        {
            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/login", content);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Login failed: {body}");

            var result = JsonSerializer.Deserialize<LoginResponse>(body);

            if (result == null || string.IsNullOrEmpty(result.token))
                throw new Exception("Login failed: invalid response");

            _session.SaveToken(result.token);

            return result;
        }

        // -----------------------------------------------------------
        // STATUS
        // -----------------------------------------------------------
        public async Task<StatusResponse> GetStatusAsync()
        {
            AttachToken();

            var response = await _http.GetAsync($"{_baseUrl}/api/status");
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                HandleUnauthorized(body);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Status error: {body}");

            return JsonSerializer.Deserialize<StatusResponse>(body)
                   ?? throw new Exception("Status parse error");
        }

        // -----------------------------------------------------------
        // KEEP ALIVE
        // -----------------------------------------------------------
        public async Task<bool> KeepAliveAsync()
        {
            AttachToken();

            var response = await _http.GetAsync($"{_baseUrl}/api/keep-alive");
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                HandleUnauthorized(body);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"KeepAlive error: {body}");

            return true;
        }

        // // -----------------------------------------------------------
        // // UPDATE FCM TOKEN
        // // -----------------------------------------------------------
        public async Task<bool> UpdateFcmTokenAsync(UpdateFcmTokenRequest req)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/update-fcm-token", content);
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                HandleUnauthorized(body);

            return response.IsSuccessStatusCode;
        }

        // -----------------------------------------------------------
        // REPORT UPDATE
        // -----------------------------------------------------------
        public async Task<bool> ReportUpdateAsync(ReportUpdateRequest req)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/report-update", content);
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                HandleUnauthorized(body);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"ReportUpdate error: {body}");

            return true;
        }

        // -----------------------------------------------------------
        // UPDATE PROMPT SEEN
        // -----------------------------------------------------------
        public async Task<bool> UpdatePromptSeenAsync(UpdatePromptSeenRequest req)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/update-prompt-seen", content);
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                HandleUnauthorized(body);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"UpdatePromptSeen error: {body}");

            return true;
        }

        // -----------------------------------------------------------
        // LOGOUT
        // -----------------------------------------------------------
        public async Task<bool> LogoutAsync()
        {
            AttachToken();

            var response = await _http.PostAsync($"{_baseUrl}/api/logout", null);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _session.ClearSession();
                return true;
            }

            _session.ClearSession();
            return response.IsSuccessStatusCode;
        }

        // -----------------------------------------------------------
        // NEW → FETCH NOTIFICATIONS
        // -----------------------------------------------------------
        public async Task<NotificationFetchResponse?> FetchNotificationsAsync()
        {
            AttachToken();

            var response = await _http.GetAsync($"{_baseUrl}/api/notifications/fetch");
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                HandleUnauthorized(body);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Notification fetch error: {body}");

            return JsonSerializer.Deserialize<NotificationFetchResponse>(body);
        }
    }
}
