using Newtonsoft.Json;
using System.Text;

namespace WebHome.Models.ViewModel
{
    public partial class QueryViewModel
    {
        public String? KeyID { get; set; }
        public String? FileDownloadToken { get; set; }
    }

    public partial class SignatureViewModel : QueryViewModel
    {
        public String? TxnID
        {
            get => KeyID;
            set => KeyID = value;
        }
        public String? DataToSign { get; set; }
        public String? DataSignature { get; set; }
        public String? Subject { get; set; }
        public int Flags { get; set; } = 1;
        public int KeyUsage { get; set; } = 0;
        public String? RemoteHost { get; set; }
        public String? Thumbprint { get; set; }
        public String? ErrorMessage { get; set; }
        public SignatureActionEnum? SignatureAction { get; set; }

    }
    public enum SignatureActionEnum
    {
        PushSignerActivation = 1,
        PopSignerActivation = 2,
    }
}
