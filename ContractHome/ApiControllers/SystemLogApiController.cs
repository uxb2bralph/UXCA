using ContractHome.Models.DataEntity;
using ContractHome.Security.Authorization;
using ContractHome.Services.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Web;

namespace ContractHome.ApiControllers
{
    /// <summary>
    /// 系統Log檔案API
    /// </summary>
    [Route("api/SystemLog")]
    [Authorize]
    [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin])]
    [ApiController]
    public class SystemLogApiController(ISystemLogService systemLogService) : ControllerBase
    {
        private readonly ISystemLogService _systemLogService = systemLogService;

        [HttpGet]
        [Route("LogList")]
        public IActionResult LogList(DateTime? dateTime = null)
        {
            dateTime ??= DateTime.Now;
            return Ok(new { DateTime = dateTime.Value.ToString("yyyy-MM-dd"), LogFiles = _systemLogService.GetLogList(dateTime) });
        }

        [HttpGet]
        [Route("DownloadLog")]
        public IActionResult DownloadLog(string fileName, DateTime? dateTime = null)
        {
            var fileBytes = _systemLogService.GetLogByte(fileName, dateTime);

            if (fileBytes == null || fileBytes.Length == 0)
            {
                return NotFound();
            }

            return File(fileBytes, "application/octet-stream", fileName);
        }
    }
}
