using System.Text.Json.Serialization;

namespace FireNet.Core.Api.Dto
{
    public class UpdateFcmTokenRequest
    {
        [JsonPropertyName("fcm_token")]
        public string FcmToken { get; set; } = "";
    }
}
