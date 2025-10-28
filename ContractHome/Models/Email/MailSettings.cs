namespace ContractHome.Models.Email
{
    public class MailSettings
    {
        public string? DisplayName { get; set; }
        public string? From { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public bool UseSSL { get; set; }
        public bool DefaultNetworkCredentials { get; set; }
        //public book UseStartTls { get; set; }
        public bool Enable { get; set; }

        public string? OPEmail { get; set; }

    }
}
