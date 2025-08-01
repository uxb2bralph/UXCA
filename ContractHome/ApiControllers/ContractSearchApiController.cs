using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
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
        [Route("SearchContract")]
        public async Task<IActionResult> SearchContract([FromBody] ContractSearchModel searchModel)
        {
            var profile = await HttpContext.GetUserAsync();
            searchModel.SearchUID = profile.UID;
            searchModel.SearchCompanyID = profile.GetCompanyID();
            var result = _contractSearchService.SearchContract(searchModel);
            return Ok(new BaseResponse()
            {
                Data = result
            });
        }
        }
}
