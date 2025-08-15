using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Services.ContractService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static ContractHome.Services.ContractService.ContractSearchDtos;

namespace ContractHome.ApiControllers
{
    [Route("api/ContractSearch")]
    [ApiController]
    [Authorize]
    public class ContractSearchApiController(IContractSearchService contractSearchService) : ControllerBase
    {
        private readonly IContractSearchService _contractSearchService = contractSearchService;

        [HttpPost]
        [Route("AllContract")]
        public async Task<IActionResult> AllContract([FromBody] ContractSearchModel searchModel)
        {
            var profile = await HttpContext.GetUserAsync();

            var result = _contractSearchService.AllContract(searchModel, profile);

            return Ok(new BaseResponse()
            {
                Data = result
            });
        }

        [HttpPost]
        [Route("WaittingContract")]
        public async Task<IActionResult> WaittingContract([FromBody] ContractSearchModel searchModel)
        {
            var profile = await HttpContext.GetUserAsync();

            var result = _contractSearchService.WaittingContract(searchModel, profile);

            return Ok(new BaseResponse()
            {
                Data = result
            });
        }
    }
}
