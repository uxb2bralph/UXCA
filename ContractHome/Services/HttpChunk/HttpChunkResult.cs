using Microsoft.VisualBasic;

namespace ContractHome.Services.HttpChunk
{
    public class HttpChunkResult
    {
        /// <summary>
        /// 處理狀態碼
        /// </summary>
        public int Code { get; set; } = 0;
        /// <summary>
        /// 處理狀態碼說明
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
