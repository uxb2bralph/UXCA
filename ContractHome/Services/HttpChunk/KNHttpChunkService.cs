
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using CommonLib.Core.Utility;
using Microsoft.Extensions.Options;

namespace ContractHome.Services.HttpChunk
{
    /// <summary>
    /// 中鋼檔案分塊上傳及下載處理
    /// </summary>
    public class KNHttpChunkService(IOptions<KNFileUploadSetting> knFileUploadSetting) : IHttpChunkService
    {
        private readonly KNFileUploadSetting _KNFileUploadSetting = knFileUploadSetting.Value;

        // 取得當前時間字串
        private static string CurrentDateTime => DateTime.Now.ToString("yyyyMMddHHmmss");
        // 對應的 Header 屬性變數
        private string FileID { get; set; } = string.Empty;
        private int ChunkIndex { get; set; } = -1;
        private int TotalChunks { get; set; } = -1;
        private string FileMD5 { get; set; } = string.Empty;
        // 檔案分片大小
        private int ChunkSize { get; set; } = 2 * 1024 * 1024; // 2MB 一片

        // 分塊 HttpRequest
        private HttpRequest? ChunkRequest { get; set; } = null;
        // 暫存資料夾路徑
        private string TempFilePath => Path.Combine(_KNFileUploadSetting.TempFolderPath, FileID + ".tmp");
        // 儲存資料夾路徑
        private string SaveFilePath => Path.Combine(_KNFileUploadSetting.DownloadFolderPath, FileID + ".pdf");

        private static void WriteLog(string message)
        {
            #if DEBUG
            Console.WriteLine($"ThreadID({Environment.CurrentManagedThreadId}):{SanitizeForLog(message)} - {CurrentDateTime}");
            #endif

            FileLogger.Logger.Info($"ThreadID({Environment.CurrentManagedThreadId}):{SanitizeForLog(message)} - {CurrentDateTime}");
        }

        private static string SanitizeForLog(string value)
        {
            if (value == null) {
                return string.Empty;
            }

            return value.Replace("\r", "").Replace("\n", "");
        }

        /// <summary>
        /// 下載分塊檔案
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        public async Task<HttpChunkResult> DownloadAsync(HttpRequest Request)
        {
            // 綁定及驗證 Header 屬性
            if (!BuildHeaderProperty(Request) && !ValidateHeaderProperty())
            {
                string msg = $"Header 屬性錯誤.{_KNFileUploadSetting.HeaderFileId} : {SanitizeForLog(FileID)} {_KNFileUploadSetting.HeaderChunkIndex}: {ChunkIndex} {_KNFileUploadSetting.HeaderTotalChunks}: {TotalChunks} {_KNFileUploadSetting.HeaderFileMD5}: {SanitizeForLog(FileMD5)}";
                WriteLog(msg);
                return new HttpChunkResult
                {
                    Code = (int)HttpChunkResultCodeEnum.HEAD_ERROR,
                    Message = msg
                };
            }

            // 將分塊資料存入暫存檔案
            await SaveChunkDataToTempFile();
            WriteLog($"Download chunk {ChunkIndex + 1}/{TotalChunks} filename: {SanitizeForLog(FileID)}");

            // 驗證MD5
            if (!ValidateMD5())
            {
                return new HttpChunkResult
                {
                    Code = (int)HttpChunkResultCodeEnum.MD5_ERROR,
                    Message = $"下載錯誤 chunk {ChunkIndex + 1}/{TotalChunks} {_KNFileUploadSetting.HeaderFileMD5}: {SanitizeForLog(FileMD5)} filename: {SanitizeForLog(FileID)}"
                };
            }

            //if (ChunkIndex + 1 == TotalChunks && ValidateContentLength())
            //{
            //    // 儲存下載檔案
            //    SaveDownloadedFile();
            //}
            // 儲存下載檔案
            SaveDownloadedFile();

            WriteLog($"Chunk Download Finish filename: {FileID}");

            WriteLog($"SavePath:{SaveFilePath} filename: {FileID}");

            return new HttpChunkResult
            {
                Code = (int)HttpChunkResultCodeEnum.COMPLETE,
                Message = SaveFilePath
            };
        }
        /// <summary>
        /// 將分塊資料存入暫存檔案
        /// </summary>
        /// <param name="Request"></param>
        /// <param name="tempFilePath"></param>
        /// <param name="chunkIndex"></param>
        /// <returns></returns>
        private async Task SaveChunkDataToTempFile()
        {
            Directory.CreateDirectory(_KNFileUploadSetting.TempFolderPath);
            using (var fs = new FileStream(TempFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
            {
                fs.Seek((long)ChunkIndex * ChunkSize, SeekOrigin.Begin);
                await ChunkRequest.Body.CopyToAsync(fs).ConfigureAwait(false);
                //await Task.Delay(500);
            }

        }

        /// <summary>
        /// 取得Header屬性
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        private bool BuildHeaderProperty(HttpRequest Request)
        {
            try
            {
                ChunkRequest = Request;
                FileID = Request.Headers[_KNFileUploadSetting.HeaderFileId].ToString();

                if (string.IsNullOrWhiteSpace(FileID) || FileID.Contains("..") || FileID.Contains('/') || FileID.Contains('\\'))
                {
                    WriteLog($"Invalid FileID detected: {FileID}");
                    return false;
                }

                ChunkIndex = int.Parse(Request.Headers[_KNFileUploadSetting.HeaderChunkIndex].ToString());
                TotalChunks = int.Parse(Request.Headers[_KNFileUploadSetting.HeaderTotalChunks].ToString());
                ChunkSize = int.Parse(Request.Headers[_KNFileUploadSetting.HeaderChunkSize].ToString());
                FileMD5 = Request.Headers[_KNFileUploadSetting.HeaderFileMD5].ToString();
                return true;
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// 驗證檔案大小是否正確
        /// </summary>
        /// <returns></returns>
        private bool ValidateContentLength()
        {
            using (var fs = new FileStream(TempFilePath, FileMode.Open))
            {
                return fs.Length % ChunkSize == ChunkRequest.ContentLength;
            }
        }

        /// <summary>
        /// 驗證Header的分塊設定是否正確
        /// </summary>
        /// <param name="Request"></param>
        /// <returns></returns>
        private bool ValidateHeaderProperty()
        {
            bool isPass = !string.IsNullOrEmpty(FileID) &&
                           ChunkIndex >= 0 &&
                           TotalChunks > 0 &&
                           ChunkIndex + 1 <= TotalChunks &&
                           !string.IsNullOrEmpty(FileMD5);
            if (!isPass)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 驗證MD5
        /// </summary>
        /// <param name="tempFilePath"></param>
        /// <param name="expectedMd5Base64"></param>
        /// <param name="fileId"></param>
        /// <returns></returns>
        private bool ValidateMD5()
        {
            using (var md5 = MD5.Create())
            using (var stream = new FileStream(TempFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] actualMd5 = md5.ComputeHash(stream);
                byte[] expectedMd5 = Convert.FromBase64String(FileMD5);

                if (!actualMd5.SequenceEqual(expectedMd5))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 儲存下載檔案
        /// </summary>
        private void SaveDownloadedFile()
        {
            Directory.CreateDirectory(_KNFileUploadSetting.DownloadFolderPath);
            var tempFile = new FileInfo(TempFilePath);
            tempFile.MoveTo(SaveFilePath, true);
        }

        /// <summary>
        /// 分塊上傳
        /// </summary>
        /// <param name="uploadFilePath"></param>
        /// <param name="uploadFileName"></param>
        /// <param name="uploadUrl"></param>
        /// <returns></returns>
        public async Task<HttpChunkResult> UploadAsync(string uploadFilePath, string uploadFileName, string uploadUrl)
        {
            var fileId = uploadFileName;
            var fileBytes = await File.ReadAllBytesAsync(uploadFilePath);
            // 計算分塊總數
            var totalChunks = (int)Math.Ceiling((double)fileBytes.Length / ChunkSize);
            var md5 = Convert.ToBase64String(MD5.HashData(fileBytes));

            WriteLog($"Chunk Upload Start filename: {fileId} - {CurrentDateTime}");

            for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
            {
                // 建立分塊資料
                var offset = chunkIndex * ChunkSize;
                var length = Math.Min(ChunkSize, fileBytes.Length - offset);
                var chunkData = new byte[length];
                Array.Copy(fileBytes, offset, chunkData, 0, length);

                // 設定 Hearder Content
                using var content = new ByteArrayContent(chunkData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Headers.ContentLength = length;
                var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
                {
                    Content = content
                };
                // 設定 Header 分片屬性
                request.Headers.Add(_KNFileUploadSetting.HeaderFileId, fileId);
                request.Headers.Add(_KNFileUploadSetting.HeaderChunkIndex, chunkIndex.ToString());
                request.Headers.Add(_KNFileUploadSetting.HeaderTotalChunks, totalChunks.ToString());
                request.Headers.Add(_KNFileUploadSetting.HeaderFileMD5, md5);
                request.Headers.Add(_KNFileUploadSetting.HeaderChunkSize, ChunkSize.ToString());

                WriteLog($"Uploaded chunk {chunkIndex + 1}/{totalChunks} filename: {fileId}");
                // 傳送
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMsg = $"上傳失敗 請求的 headers.{_KNFileUploadSetting.HeaderFileId} : {fileId} {_KNFileUploadSetting.HeaderChunkIndex}: {chunkIndex} {_KNFileUploadSetting.HeaderTotalChunks}: {totalChunks} {_KNFileUploadSetting.HeaderFileMD5}: {md5} Response_StatusCode: {response.StatusCode}";

                        WriteLog(errorMsg);

                        return new HttpChunkResult
                        {
                            Code = (int)HttpChunkResultCodeEnum.HTTP_ERROR,
                            Message = errorMsg
                        };
                    }
                }
            }

            WriteLog($"Chunk Upload Finish filename: {fileId}");

            return new HttpChunkResult
            {
                Code = (int)HttpChunkResultCodeEnum.COMPLETE,
                Message = $"檔案 {_KNFileUploadSetting.HeaderFileId}: {fileId} 上傳成功."
            };
        }
    }
}
