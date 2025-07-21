using ContractHome.Models.DataEntity;
using ContractHome.Security.Authorization;
using ContractHome.Services.ContractCategroyManage;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Wangkanai.Extensions;

namespace ContractHome.ApiControllers
{
    [Route("api/ContractCategroy")]
    [ApiController]
    [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
    public class ContractCategroyApiController(IContractCategoryService contractCategroyService) : ControllerBase
    {
        private readonly IContractCategoryService _contractCategroyService = contractCategroyService;

        [HttpPost]
        [Route("Create")]
        public IActionResult Create([FromBody] ContractCategoryCreateRequest request)
        {
            request.CreateUID = 39;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            contractCategroyService.CreateContractCategroy(request);

            return Ok();
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
            return Ok();
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
            return Ok();
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
            return Ok(result);
        }
    }
}
