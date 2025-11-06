using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using CommonLib.Core.Utility;
using ContractHome.Services.HttpChunk;
using Microsoft.Extensions.Options;

namespace ContractHome.Helper
{
    /// <summary>
    /// 分塊檔案上傳 將檔案分塊後 同時上傳至Server 並通知Server合併檔案
    /// </summary>
    public class ChunkFileUploader(IOptions<KNFileUploadSetting> kNFileUploadSetting, IHttpClientFactory httpClientFactory)
    {
        private readonly KNFileUploadSetting _kNFileUploadSetting = kNFileUploadSetting.Value;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        const int MaxRetries = 3;
        const int ThreadPoolSize = 4;

        const string HttpClientName = "GatewayClient";

        //static readonly HttpClient httpClient = new()
        //{
        //    Timeout = TimeSpan.FromSeconds(30)
        //};

        // 取得當前時間字串
        private static string CurrentDateTime => DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

        private static void WriteLog(string message)
        {
            #if DEBUG
            Console.WriteLine($"ThreadID({Environment.CurrentManagedThreadId}):{message} - {CurrentDateTime}");
            #endif

            FileLogger.Logger.Info($"ThreadID({Environment.CurrentManagedThreadId}):{message} - {CurrentDateTime}");
        }

        private bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;
            if (identifier.Contains("..") || identifier.Contains('/') || identifier.Contains('\\'))
                return false;
            // Only allow alphanumeric, dash, and underscore for identifier (adjust pattern as needed)
            foreach (char c in identifier)
            {
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task UploadAsync(string filePath)
        {
            if (!_kNFileUploadSetting.Enable)
            {
                WriteLog($"kNFileUploadSetting.Enable:{_kNFileUploadSetting.Enable}");
                return;
            }

            string fileId = await GenerateFileId(filePath);
            var fileInfo = new FileInfo(filePath);
            long fileSize = fileInfo.Length;
            int totalChunks = (int)Math.Ceiling((double)fileSize / _kNFileUploadSetting.ChunkSize);

            WriteLog($"fileId={fileId} 上傳檔案: {fileInfo.Name} (大小: {fileSize / (1024 * 1024)} MB, 分塊數: {totalChunks})");

            var missingChunks = await CheckUploadStatus(fileId, totalChunks);
            // 分段檔案還在 傳送重組通知
            if (missingChunks.Count == 0)
            {
                WriteLog($"fileId={fileId}-{fileInfo.Name}-檔案已完整上傳 通知重組");
                await NotifyAssemble(fileId, fileInfo.Name);
                return;
            }

            var progress = new ConcurrentDictionary<int, bool>();
            using var semaphore = new SemaphoreSlim(ThreadPoolSize);

            var tasks = new List<Task>();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            foreach (int chunkIndex in missingChunks)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await UploadChunkWithRetry(fs, fileId, chunkIndex, totalChunks);
                        progress[chunkIndex] = true;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            //// 進度檢查
            //_ = Task.Run(async () =>
            //{
            //    while (progress.Count < missingChunks.Count)
            //    {
            //        Console.WriteLine($"進度：{progress.Count}/{missingChunks.Count}");
            //        await Task.Delay(1000);
            //    }
            //    Console.WriteLine("上傳完成！");
            //});

            await Task.WhenAll(tasks);

            await NotifyAssemble(fileId, fileInfo.Name);
        }
        /// <summary>
        /// 檢查上傳狀態
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="totalChunks"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<HashSet<int>> CheckUploadStatus(string fileId, int totalChunks)
        {
            var url = $"{_kNFileUploadSetting.ChunkUploadUrl}/status?identifier={fileId}&totalChunks={totalChunks}";
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            var resp = await httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"無法取得狀態: {resp.StatusCode}");
            }
            var json = await resp.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var result = new HashSet<int>();
            if (doc.RootElement.TryGetProperty("missingChunks", out var chunks))
            {
                foreach (var node in chunks.EnumerateArray())
                    result.Add(node.GetInt32());
            }
            return result;
        }
        /// <summary>
        /// 上傳檔案分塊
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="fileId"></param>
        /// <param name="chunkIndex"></param>
        /// <param name="totalChunks"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task UploadChunkWithRetry(FileStream fs, string fileId, int chunkIndex, int totalChunks)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    long offset = (long)chunkIndex * _kNFileUploadSetting.ChunkSize;
                    int length = (int)Math.Min(_kNFileUploadSetting.ChunkSize, fs.Length - offset);
                    byte[] buffer = new byte[length];

                    lock (fs)
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        fs.Read(buffer, 0, length);
                    }

                    var content = BuildMultipartContent(fileId, chunkIndex, totalChunks, buffer);
                    var httpClient = _httpClientFactory.CreateClient(HttpClientName);
                    var resp = await httpClient.PostAsync(_kNFileUploadSetting.ChunkUploadUrl, content);

                    if (resp.IsSuccessStatusCode)
                    {
                        WriteLog($"fileId:{fileId} Chunk {chunkIndex} 成功：{resp.StatusCode} - 嘗試 {attempt}/{MaxRetries}");
                        return;
                    }

                    WriteLog($"[!] fileId:{fileId} Chunk {chunkIndex} 失敗：{resp.StatusCode} - 嘗試 {attempt}/{MaxRetries}");
                }
                catch (Exception ex)
                {
                    WriteLog($"[!] fileId:{fileId} Chunk {chunkIndex} 發生錯誤：{ex.Message} - 嘗試 {attempt}/{MaxRetries}");
                }
            }

            throw new Exception($"Chunk {chunkIndex} 上傳失敗");
        }
        /// <summary>
        /// 建立分塊內容
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="chunkIndex"></param>
        /// <param name="totalChunks"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private MultipartFormDataContent BuildMultipartContent(string fileId, int chunkIndex, int totalChunks, byte[] data)
        {
            var content = new MultipartFormDataContent();
            var byteContent = new ByteArrayContent(data);
            byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(byteContent, "file", "chunk");
            content.Add(new StringContent(chunkIndex.ToString()), "chunkNumber");
            content.Add(new StringContent(totalChunks.ToString()), "totalChunks");
            content.Add(new StringContent(fileId), "identifier");
            return content;
        }
        /// <summary>
        /// 通知伺服器合併檔案
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="originalFileName"></param>
        /// <returns></returns>
        private async Task NotifyAssemble(string fileId, string originalFileName)
        {
            var url = $"{_kNFileUploadSetting.ChunkUploadUrl}/assemble";
            var content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("identifier", fileId),
                new KeyValuePair<string, string>("originalFilename", originalFileName)
            ]);
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);
            var resp = await httpClient.PostAsync(url, content);
            var result = await resp.Content.ReadAsStringAsync();
            WriteLog("伺服器回應：" + result);
        }
        /// <summary>
        /// 產生檔案 ID
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private async Task<string> GenerateFileId(string filePath)
        {
            using var md5 = MD5.Create();
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] hash = await md5.ComputeHashAsync(fs);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        /// <summary>
        /// 儲存分塊檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="chunkNumber"></param>
        /// <param name="totalChunks"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public async Task<bool> SaveChunkFileAsync(IFormFile file, int chunkNumber, int totalChunks, string identifier)
        {
            if (!IsValidIdentifier(identifier))
                throw new ArgumentException("Invalid identifier: Path traversal or unsafe characters detected.", nameof(identifier));

            var tempDir = Path.Combine(_kNFileUploadSetting.TempFolderPath);
            Directory.CreateDirectory(tempDir);

            var chunkFileName = $"{identifier}_{chunkNumber}.part";
            var chunkPath = Path.Combine(tempDir, chunkFileName);

            using var fs = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);
            await file.CopyToAsync(fs);

            return IsUploadComplete(identifier, totalChunks);
        }
        /// <summary>
        /// 檢查上傳是否完成
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="totalChunks"></param>
        /// <returns></returns>
        private bool IsUploadComplete(string identifier, int totalChunks)
        {
            if (!IsValidIdentifier(identifier))
                throw new ArgumentException("Invalid identifier: Path traversal or unsafe characters detected.", nameof(identifier));

            var tempDir = Path.Combine(_kNFileUploadSetting.TempFolderPath);
            for (int i = 0; i < totalChunks; i++)
            {
                var chunkPath = Path.Combine(tempDir, $"{identifier}_{i}.part");
                if (!System.IO.File.Exists(chunkPath))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 組合檔案
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="originalFilename"></param>
        /// <returns></returns>
        public async Task<(string outputPath, string outputFilename)> AssembleFile(string identifier, string originalFilename)
        {
            if (!IsValidIdentifier(identifier))
                throw new ArgumentException("Invalid identifier: Path traversal or unsafe characters detected.", nameof(identifier));

            var chunkPaths = GetSortedChunks(identifier);
            var tempFilename = string.IsNullOrWhiteSpace(originalFilename)
                ? $"assembled_{identifier}"
                : originalFilename;
            var tempFilePath = Path.Combine(_kNFileUploadSetting.TempFolderPath, tempFilename);

            await using var output = new FileStream(tempFilePath, FileMode.Create);
            foreach (var chunk in chunkPaths)
            {
                await using var input = new FileStream(chunk, FileMode.Open);
                await input.CopyToAsync(output);
            }
            output.Close();
            output.Dispose();

            // 刪除所有分塊檔案
            foreach (var chunk in chunkPaths)
            {
                File.Delete(chunk);
            }

            // 搬移temp檔案至下載資料夾
            var tempFile = new FileInfo(tempFilePath);
            string downloadFilePath = Path.Combine(_kNFileUploadSetting.DownloadFolderPath, tempFile.Name);
            tempFile.MoveTo(downloadFilePath, true);

            return (downloadFilePath, tempFilename);
        }
        /// <summary>
        /// 取得已排序的分塊檔案
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private List<string> GetSortedChunks(string identifier)
        {
            var tempDir = Path.Combine(_kNFileUploadSetting.TempFolderPath);
            var allChunks = Directory
                .GetFiles(tempDir, $"{identifier}_*.part")
                .OrderBy(path => ExtractChunkIndex(Path.GetFileName(path)))
                .ToList();

            return allChunks;
        }
        /// <summary>
        /// 從檔名中提取分塊索引
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private int ExtractChunkIndex(string fileName)
        {
            var part = fileName.Substring(fileName.LastIndexOf('_') + 1);
            return int.Parse(part.Replace(".part", ""));
        }
        /// <summary>
        /// 取得缺失的分塊索引
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="totalChunks"></param>
        /// <returns></returns>
        public List<int> GetMissingChunks(string identifier, int totalChunks)
        {
            if (string.IsNullOrEmpty(identifier) ||
                identifier.Contains("..") ||
                identifier.Contains('/') ||
                identifier.Contains('\\'))
            {
                throw new ArgumentException("Invalid identifier value.");
            }

            var tempDir = Path.Combine(_kNFileUploadSetting.TempFolderPath);
            var missingChunks = new List<int>();
            for (int i = 0; i < totalChunks; i++)
            {
                var chunkPath = Path.Combine(tempDir, $"{identifier}_{i}.part");
                if (!File.Exists(chunkPath))
                {
                    missingChunks.Add(i);
                }
            }
            return missingChunks;
        }

        /// <summary>
        /// 驗證檔案的 MD5 雜湊值
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="expectedMd5"></param>
        /// <returns></returns>
        private bool VerifyMd5(string filePath, string expectedMd5)
        {
            using var md5 = MD5.Create();
            using var stream = System.IO.File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            var actualMd5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            return string.Equals(expectedMd5, actualMd5, StringComparison.OrdinalIgnoreCase);
        }
    }
}
