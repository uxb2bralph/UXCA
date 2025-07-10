namespace ContractHome.Services.HttpChunk
{
    public interface IHttpChunkService
    {
        Task<HttpChunkResult> DownloadAsync(HttpRequest httpRequest);
        Task<HttpChunkResult> UploadAsync(string uploadFilePath, string uploadFileName, string uploadUrl);
    }
}
