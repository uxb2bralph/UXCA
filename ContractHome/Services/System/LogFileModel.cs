using System.Web;

namespace ContractHome.Services.System
{
    public class LogFileModel
    {
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string DateTime { get; set; } = string.Empty;
        public string DownLoadUrl => $"/api/SystemLog/DownloadLog?fileName={HttpUtility.UrlEncode(FileName)}&dateTime={DateTime}";
    }
}
