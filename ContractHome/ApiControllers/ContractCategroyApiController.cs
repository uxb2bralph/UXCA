using ContractHome.Services.ContractCategroyManage;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Wangkanai.Extensions;

namespace ContractHome.ApiControllers
{
    [Route("api/ContractCategroy")]
    //[Authorize]
    //[RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin, (int)UserRoleDefinition.RoleEnum.MemberAdmin])]
    [ApiController]
    public class ContractCategroyApiController(IContractCategroyService contractCategroyService) : ControllerBase
    {
        private readonly IContractCategroyService _contractCategroyService = contractCategroyService;

        [HttpPost]
        [Route("Create")]
        public IActionResult Create([FromBody] ContractCategroyCreateRequest request)
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
        public IActionResult Modify([FromBody] ContractCategroyModifyRequest request)
        {
            request.ModifyUID = 39;

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            contractCategroyService.ModifyContractCategroy(request);
            return Ok();
        }
    }
}
