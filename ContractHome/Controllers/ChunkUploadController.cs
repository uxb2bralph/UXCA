using System.Security.Cryptography;
using CommonLib.Core.Utility;
using ContractHome.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractHome.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChunkUploadController(ChunkFileUploader chunkFileUploader) : ControllerBase
    {
        private readonly ChunkFileUploader _chunkFileUploader = chunkFileUploader;

        /// <summary>
        /// 接收檔案
        /// </summary>
        /// <param name="file"></param>
        /// <param name="chunkNumber"></param>
        /// <param name="totalChunks"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> UploadChunk(
            IFormFile file,
            [FromForm] int chunkNumber,
            [FromForm] int totalChunks,
            [FromForm] string identifier)
        {
            try
            {
                bool isUploadComplete = await _chunkFileUploader.SaveChunkFileAsync(file, chunkNumber, totalChunks, identifier);

                var response = new Dictionary<string, object>
                {
                    ["message"] = "分塊上傳成功",
                    ["chunkNumber"] = chunkNumber,
                    ["uploadComplete"] = isUploadComplete
                };

                if ((bool)response["uploadComplete"])
                {
                    response["assembleUrl"] = $"/api/chunkedupload/assemble?identifier={identifier}";
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }
        /// <summary>
        /// 重組Temp檔案
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="originalFilename"></param>
        /// <returns></returns>
        [HttpPost("assemble")]
        public async Task<IActionResult> AssembleFile([FromForm] string identifier, [FromForm] string? originalFilename)
        {
            try
            {
                var (outputPath, outputFilename) = await _chunkFileUploader.AssembleFile(identifier, originalFilename);

                return Ok(new
                {
                    message = "檔案組合完成",
                    filename = outputFilename,
                    size = new FileInfo(outputPath).Length,
                    downloadUrl = $"/api/chunkedupload/download?filename={outputFilename}"
                });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return StatusCode(500, new { error = ex.Message });
            }
        }
        /// <summary>
        /// 檢查檔案是否漏傳
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="totalChunks"></param>
        /// <returns></returns>
        [HttpGet("status")]
        public IActionResult GetUploadStatus([FromQuery] string identifier, [FromQuery] int totalChunks)
        {
            try
            {
                var missingChunks = _chunkFileUploader.GetMissingChunks(identifier, totalChunks);
                return Ok(new { missingChunks });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex);
                return StatusCode(500, new { error = ex.Message });
            }

        }
    }
}
