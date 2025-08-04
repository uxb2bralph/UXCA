using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Security.Authorization;
using ContractHome.Services.ContractCategroyManage;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wangkanai.Extensions;

namespace ContractHome.ApiControllers
{
    [Route("api/ContractCategory")]
    [ApiController]
    [Authorize]
    public class ContractCategoryApiController(IContractCategoryService contractCategroyService) : ControllerBase
    {
        private readonly IContractCategoryService _contractCategroyService = contractCategroyService;

        [HttpPost]
        [Route("Create")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
        public async Task<IActionResult> CreateAsync([FromBody] ContractCategoryCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var profile = await HttpContext.GetUserAsync();
            request.CompanyID = profile.CurrentCompanyID;
            request.CreateUID = profile.UID;

            contractCategroyService.CreateContractCategroy(request);

            return Ok(new BaseResponse());
        }

        [HttpPost]
        [Route("Modify")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
        public async Task<IActionResult> ModifyAsync([FromBody] ContractCategoryModifyRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var profile = await HttpContext.GetUserAsync();
            request.CompanyID = profile.CurrentCompanyID;
            request.ModifyUID = profile.UID;

            contractCategroyService.ModifyContractCategroy(request);
            return Ok(new BaseResponse());
        }

        [HttpPost]
        [Route("Delete")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
        public IActionResult Delete([FromBody] ContractCategoryDeleteRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            contractCategroyService.DeleteContractCategroy(request);
            return Ok(new BaseResponse());
        }

        [HttpPost]
        [Route("Query")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
        public IActionResult Query([FromBody] ContractCategoryQueryModel request)
        {
            var result = contractCategroyService.QuertyContractCategory(request);

            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpPost]
        [Route("GetCompanyUsers")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
        public IActionResult GetCompanyUsers([FromBody] ContractCategoryQueryModel request)
        {
            var result = contractCategroyService.GetCompanyUsers(request);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpPost]
        [Route("GetContractCategoryInfo")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
        public IActionResult GetContractCategoryInfo([FromBody] ContractCategoryQueryModel request)
        {
            var result = contractCategroyService.GetContractCategoryInfo(request);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpGet]
        [Route("GetContractCategoryOption")]
        public async Task<IActionResult> GetContractCategoryOptionAsync()
        {
            var profile = await HttpContext.GetUserAsync();
            var result = contractCategroyService.GetContractCategoryOption(profile.UID, profile.CurrentCompanyID);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }
    }
}
