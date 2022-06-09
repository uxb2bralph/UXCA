namespace WebHome.Properties
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
        public bool UseGoogleAuthenticator { get; set; } = false;
        public int EIVO_Service { get; set; } = 1;
        public string DefaultUILanguage { get; set; } = "zh-TW";
        public string ThermalPOS { get; set; } = "0 0 162 792";
        public String TaskCenter { get; set; } = "TaskCenter";
    }

}
