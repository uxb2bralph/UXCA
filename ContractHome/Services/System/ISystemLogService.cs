namespace ContractHome.Services.System
{
    /// <summary>
    /// 系統Log檔案服務
    /// </summary>
    public interface ISystemLogService
    {
        /// <summary>
        /// 取得指定日期的Log檔案列表
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public List<LogFileModel> GetLogList(DateTime? dateTime = null);
        /// <summary>
        /// 取得Log檔案的位元組陣列
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public byte[] GetLogByte(string fileName, DateTime? dateTime = null);
    }
}
