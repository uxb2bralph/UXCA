using TWCACAPIAdapter.Properties;

namespace TWCACAPIAdapter.Models.ViewModel
{
    public class TWCASignDataViewModel
    {
        public String? DataToSign { get; set; }
        public String? Subject { get; set; }
        public int Flags { get; set; } = 1;
        public int KeyUsage { get; set; } = 0;
    }

    public enum KeyStoreType
    {
        PC = 1,
        eToken = 2,
    }
    public class TWCAPKCSViewModel
    {
        public String? FirstName { get; set; }
        public String OU { get; set; } = Settings.Default.OU;
        public String O { get; set; } = Settings.Default.O;
        public KeyStoreType? KeyStore { get; set; }
        public String Country { get; set; } = Settings.Default.Country;
        public String? CSR { get; set; }
        public String? Pkcs7 { get; set; }
    }

}
