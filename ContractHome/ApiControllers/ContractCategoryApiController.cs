using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Security.Authorization;
using ContractHome.Services.ContractCategroyManage;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Wangkanai.Extensions;

namespace ContractHome.ApiControllers
{
    [Route("api/ContractCategory")]
    [ApiController]
    [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
    public class ContractCategoryApiController(IContractCategoryService contractCategroyService) : ControllerBase
    {
        private readonly IContractCategoryService _contractCategroyService = contractCategroyService;

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> CreateAsync([FromBody] ContractCategoryCreateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var profile = await HttpContext.GetUserAsync();
            request.CompanyID = profile.GetCompanyID();
            request.CreateUID = profile.UID;

            contractCategroyService.CreateContractCategroy(request);

            return Ok(new BaseResponse());
        }

        [HttpPost]
        [Route("Modify")]
        public IActionResult Modify([FromBody] ContractCategoryModifyRequest request)
        {
            request.ModifyUID = 39;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            contractCategroyService.ModifyContractCategroy(request);
            return Ok(new BaseResponse());
        }

        [HttpPost]
        [Route("Delete")]
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
        public IActionResult Query([FromBody] ContractCategoryQueryModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var result = contractCategroyService.QuertyContractCategory(request);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpPost]
        [Route("GetCompanyUsers")]
        public IActionResult GetCompanyUsers([FromBody] ContractCategoryQueryModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var result = contractCategroyService.GetCompanyUsers(request);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }
    }
}
