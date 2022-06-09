namespace TWCACAPIAdapter.Properties
{
    public class Settings : CommonLib.Utility.Properties.AppSettings
    {
        static Settings _default;

        public static Settings Default => _default;

        static Settings()
        {
            _default = Initialize<Settings>(typeof(Settings).Namespace);
        }

        public String ApplicationPath { get; set; } = "";
        public double SessionTimeoutInMinutes { get; set; } = 20;
        public double LoginExpireMinutes { get; set; } = 1440 * 7;
        public String LoginUrl { get; set; } = "/Account/Login";
        public String LogoutUrl { get; set; } = "/Account/Logout";
        public String HostDomain { get; set; } = "http://localhost:5000";
        public String OU { get; set; } = "70762419UXRA";
        public String O { get; set; } = "Chunghwa Telecom";
        public String Country { get; set; } = "TW";
        public String RootCA { get; set; } = "ca.crt";
        public String[] CORS_Origins { get; set; } = { "http://192.168.200.16:8880", "https://www.uxcacenter.com", "http://localhost" };
    }

}
