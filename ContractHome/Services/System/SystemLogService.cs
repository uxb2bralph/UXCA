namespace ContractHome.Services.System
{
    /// <summary>
    /// 系統Log檔案Service
    /// </summary>
    public class SystemLogService : ISystemLogService
    {
        /// <summary>
        /// 取得Log檔案的位元組陣列
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public byte[] GetLogByte(string fileName, DateTime? dateTime = null)
        {
            dateTime ??= DateTime.Now;
            string path = Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "logs",
                                $"{dateTime:yyyy}", $"{dateTime:MM}", $"{dateTime:dd}");
            if (!Directory.Exists(path))
            {
                return [];
            }
            string filePath = Path.Combine(path, fileName);

            if (!File.Exists(filePath))
            {
                return [];
            }
            return File.ReadAllBytes(filePath);
        }
        /// <summary>
        /// 取得指定日期的Log檔案列表
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public List<LogFileModel> GetLogList(DateTime? dateTime = null)
        {
            dateTime ??= DateTime.Now;

            string path = Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "logs",
                                $"{dateTime:yyyy}", $"{dateTime:MM}", $"{dateTime:dd}");

            if (!Directory.Exists(path))
            {
                return [];
            }

            var logFiles = new List<LogFileModel>();
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                logFiles.Add(
                    new LogFileModel
                    {
                        FileName = fileInfo.Name,
                        FileSize = $"{fileInfo.Length / 1024} KB",
                        DateTime = dateTime.Value.ToString("yyyy-MM-dd")
                    });
            }

            return logFiles;
        }
    }
}
