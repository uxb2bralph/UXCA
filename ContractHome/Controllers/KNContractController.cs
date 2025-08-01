using CommonLib.Core.Utility;
using ContractHome.Models.DataEntity;
using ContractHome.Security.Authorization;
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

        [HttpGet]
        [Route("UploadPdfFile")]
        [RoleAuthorize(roleID: [(int)UserRoleDefinition.RoleEnum.SystemAdmin])]
        public async Task<IActionResult> UploadPdfFile(string contractNo)
        {
            try
            {
                DCDataContext db = new DCDataContext();
                var contract = db.GetTable<Contract>().Where(x => x.ContractNo == contractNo).FirstOrDefault();
                if (contract == null)
                {
                    return NotFound("Contract not found");
                }

                bool isSuccess = await _customContractService.UploadSignatureAndFootprintsPdfFile(contract);

                return Ok(new ContractResultModel()
                {
                    msgRes = new MsgRes()
                    {
                        type = (isSuccess) ? ContractResultType.S.ToString() : ContractResultType.F.ToString(),
                        code = (isSuccess) ? ContractResultCode.Success.GetFullCode() : ContractResultCode.ContractUpdate.GetFullCode(),
                        desc = (isSuccess) ? "上傳成功" : "上傳失敗"
                    }
                });
            }
            catch (Exception ex)
            {
                FileLogger.Logger.Error(ex.ToString());
                return Ok(new ContractResultModel()
                {
                    msgRes = new MsgRes()
                    {
                        type = ContractResultType.E.ToString(),
                        code = ContractResultCode.ContractUpdate.GetFullCode(),
                        desc = ex.ToString()
                    }
                });
            }
        }

    }
}
