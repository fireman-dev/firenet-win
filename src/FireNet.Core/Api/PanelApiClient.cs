using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FireNet.Core.Api.Dto;
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
            string token = _session.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        // -----------------------------------------------------------
        // 1) LOGIN
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

            _session.SaveToken(result.token);

            return result;
        }

        // -----------------------------------------------------------
        // 2) STATUS
        // -----------------------------------------------------------
        public async Task<StatusResponse> GetStatusAsync()
        {
            AttachToken();

            var response = await _http.GetAsync($"{_baseUrl}/api/status");
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _session.ClearSession();
                throw new Exception("Token expired");
            }

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Status error: {body}");

            return JsonSerializer.Deserialize<StatusResponse>(body);
        }

        // -----------------------------------------------------------
        // 3) KEEP ALIVE
        // -----------------------------------------------------------
        public async Task<bool> KeepAliveAsync()
        {
            AttachToken();

            var response = await _http.PostAsync($"{_baseUrl}/api/keepalive", null);
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _session.ClearSession();
                return false;
            }

            return response.IsSuccessStatusCode;
        }

        // -----------------------------------------------------------
        // 4) UPDATE FCM TOKEN
        // -----------------------------------------------------------
        public async Task<bool> UpdateFcmTokenAsync(UpdateFcmTokenRequest req)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/update-fcm-token", content);
            string body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _session.ClearSession();
                return false;
            }

            return response.IsSuccessStatusCode;
        }

        // -----------------------------------------------------------
        // 5) REPORT UPDATE
        // -----------------------------------------------------------
        public async Task<bool> ReportUpdateAsync(ReportUpdateRequest req)
        {
            AttachToken();

            var json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{_baseUrl}/api/report-update", content);

            return response.IsSuccessStatusCode;
        }

        // -----------------------------------------------------------
        // 6) UPDATE PROMPT SEEN
        // -----------------------------------------------------------
        public async Task<bool> UpdatePromptSeenAsync()
        {
            AttachToken();

            var response = await _http.PostAsync($"{_baseUrl}/api/update-prompt-seen", null);
            return response.IsSuccessStatusCode;
        }

        // -----------------------------------------------------------
        // 7) LOGOUT
        // -----------------------------------------------------------
        public async Task<bool> LogoutAsync()
        {
            AttachToken();

            var response = await _http.PostAsync($"{_baseUrl}/api/logout", null);

            _session.ClearSession();

            return response.IsSuccessStatusCode;
        }
    }
}
