namespace ContractHome.Services.HttpChunk
{
    /// <summary>
    /// 中鋼kn合約上傳設定
    /// </summary>
    public class KNFileUploadSetting
    {
        /// <summary>
        /// 是否啟用分段上傳功能
        /// </summary>
        public bool Enable { get; set; } = false;
        /// <summary>
        /// 分段上傳URL
        /// </summary>
        public string ChunkUploadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 暫存資料夾名稱
        /// </summary>
        public string TempFolderPath { get; set; } = string.Empty;
        /// <summary>
        /// 下載資料夾名稱
        /// </summary>
        public string DownloadFolderPath { get; set; } = string.Empty;
        /// <summary>
        /// Chunk檔案上傳大小
        /// </summary>
        public long ChunkSize { get; set; } = 2097152; // 2 * 1024 * 1024 = 2MB
        /// <summary>
        /// 合約檔案 EzoMQ QueueID
        /// </summary>
        public string ContractQueueid { get; set; } = string.Empty;
        /// <summary>
        /// 簽署檔案 EzoMQ QueueID
        /// </summary>
        public string SignatureQueueid { get; set; } = string.Empty;
        /// <summary>
        /// 簽署軌跡檔 EzoMQ QueueID
        /// </summary>
        public string HistoryQueueid { get; set; } = string.Empty;
        /// <summary>
        /// 上傳的檔案名稱
        /// </summary>
        public string HeaderFileId { get; set; } = string.Empty;
        /// <summary>
        /// MD5值
        /// </summary>
        public string HeaderFileMD5 { get; set; } = string.Empty;
        /// <summary>
        /// Chunk 索引
        /// </summary>
        public string HeaderChunkIndex { get; set; } = string.Empty;
        /// <summary>
        /// Chunk 總數量
        /// </summary>
        public string HeaderTotalChunks { get; set; } = string.Empty;
        /// <summary>
        /// 每次上傳的 Chunk 大小
        /// </summary>
        public string HeaderChunkSize { get; set; } = string.Empty;
        /// <summary>
        /// 上傳檔案的時間戳記
        /// </summary>
        public string FileCurrentDateTime => DateTime.Now.ToString("yyyyMMddHHmmfff");
    }
}
