namespace FireNet.Core.Api.Dto
{
    public class LoginRequest
    {
        public string username { get; set; }
        public string password { get; set; }
        public string device_id { get; set; }
        public string app_version { get; set; }
    }
}
