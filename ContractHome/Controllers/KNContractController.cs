using CommonLib.Core.Utility;
using ContractHome.Services.ContractService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ContractHome.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KNContractController(ICustomContractService customContractService) : ControllerBase
    {
        private readonly ICustomContractService _customContractService = customContractService;

        [HttpPost]
        [Route("CreateContract")]
        public async Task<IActionResult> CreateContract([FromBody] ContractModel contractModel)
        {
            try
            {
                if (!_customContractService.IsValid(contractModel, ModelState, out ContractResultModel result))
                {
                    return Ok(result);
                }
                var createResult = _customContractService.CreateContract(contractModel);
                return Ok(createResult);
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex.ToString());
                return Ok(new ContractResultModel()
                {
                    msgRes = new MsgRes()
                    {
                        type = ContractResultType.E.ToString(),
                        code = ContractResultCode.ContractCreate.GetFullCode(),
                        desc = ex.ToString()
                    }
                });
            }

        }
    }
}
