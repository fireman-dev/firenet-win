using System;
using System.Text.Json.Serialization;

namespace FireNet.Core.Api.Dto.Notifications
{
    public class NotificationItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("body")]
        public string Body { get; set; } = "";

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }
}
