using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FireNet.Core.Api.Dto.Notifications
{
    public class NotificationFetchResponse
    {
        [JsonPropertyName("notifications")]
        public List<NotificationItem> Notifications { get; set; } = new();

        [JsonPropertyName("check_time")]
        public DateTime CheckTime { get; set; }
    }
}
