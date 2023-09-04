using TWCACAPIAdapter.Properties;

namespace TWCACAPIAdapter.Models.ViewModel
{
    public class TWCASignDataViewModel
    {
        public String? DataToSign { get; set; }
        public String? Subject { get; set; }
        public int Flags { get; set; } = 1;
        public int KeyUsage { get; set; } = 0;
        public String? TxnID { get; set; }
        public String? RemoteHost { get; set; }
        public String? Thumbprint { get; set; }
        public String? ErrorMessage { get; set; }
        public String? DataSignature { get; set; }
        public SignatureActionEnum? SignatureAction { get; set; }

    }
    public enum SignatureActionEnum
    {
        PushSignerActivation = 1,
        PopSignerActivation = 2,
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
