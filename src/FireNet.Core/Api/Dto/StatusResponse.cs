using System.Collections.Generic;

namespace FireNet.Core.Api.Dto
{
    public class StatusResponse
    {
        public string username { get; set; }
        public long used_traffic { get; set; }
        public long data_limit { get; set; }
        public long expire { get; set; }
        public string status { get; set; }
        public List<string> links { get; set; }
        public string need_to_update { get; set; }
        public string is_ignoreable { get; set; }
    }
}
