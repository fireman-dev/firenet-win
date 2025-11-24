using System.Text.Json.Serialization;

namespace FireNet.Core.Api.Dto
{
    public class UpdateFcmTokenRequest
    {
        [JsonPropertyName("fcm_token")]
        public string fcm_token { get; set; } = "";
    }
}
