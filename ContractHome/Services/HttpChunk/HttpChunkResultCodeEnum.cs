namespace ContractHome.Services.HttpChunk
{
    /// <summary>
    /// Http Chunk 上傳狀態碼
    /// </summary>
    public enum HttpChunkResultCodeEnum
    {
        /// <summary>
        /// 成功
        /// </summary>
        COMPLETE = 0,
        /// <summary>
        /// Http Header 錯誤
        /// </summary>
        HEAD_ERROR = 1,
        /// <summary>
        /// MD5 錯誤
        /// </summary>
        MD5_ERROR = 2,
        /// <summary>
        /// Http 錯誤
        /// </summary>
        HTTP_ERROR = 3,
        /// <summary>
        /// 系統錯誤
        /// </summary>
        SYSTEM_ERROR = 4
    }
}
