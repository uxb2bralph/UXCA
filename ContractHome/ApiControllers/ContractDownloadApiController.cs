using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Helper;
using ContractHome.Models.ViewModel;
using ContractHome.Services.ContractService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Web;
using Wangkanai.Detection.Services;
using static ContractHome.Helper.JwtTokenGenerator;

namespace ContractHome.ApiControllers
{
    [Route("api/ContractDownload")]
    [ApiController]
    public class ContractDownloadApiController(DCDataContext db, ContractServices contractServices, ICustomContractService _customContractService, IDetectionService detectionService) : ControllerBase
    {
        private readonly DCDataContext _db = db;
        private readonly ContractServices _contractServices = contractServices;
        private readonly ICustomContractService _customContractService = _customContractService;
        private readonly IDetectionService _detectionService = detectionService;

        private async Task<Contract?> GetContractAsync(string token, bool isDownloadContract = true)
        {
            (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile)
                    = _contractServices.TokenDownloadValidate(JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData());
            if (resp.HasError)
            {
                throw new ArgumentException(resp.Message);
            }
            if (string.IsNullOrEmpty(jwtTokenObj.ContractID))
            {
                throw new ArgumentException("contractID is null.");
            }
            
            var contract = _db.GetTable<Contract>()
                            .Where(c => c.ContractID == jwtTokenObj.ContractID.DecryptKeyValue())
                            .FirstOrDefault();
            if (contract == null)
            {
                return null;
            }

            var profile = await HttpContext.GetUserAsync();

            if (profile == null)
            {
                return null;
            } 

            contract.CDS_Document.DocumentProcessLog.Add(new DocumentProcessLog
            {
                LogDate = DateTime.Now,
                ActorID = jwtTokenObj.UID.DecryptKeyValue(),
                StepID = (isDownloadContract) ? (int)CDS_Document.StepEnum.DownloadContract : (int)CDS_Document.StepEnum.DownloadFootprint,
                ClientIP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ClientDevice = $"{_detectionService.Platform.Name} {_detectionService.Platform.Version.ToString()}/{_detectionService.Browser.Name}"
            });

            db.SubmitChanges();
            return contract;
        }

        [HttpGet]
        [Route("DownloadContract")]
        [Authorize]
        public async Task<IActionResult> DownloadContractAsync(string token)
        {
            Contract? contract = await GetContractAsync(token, true);

            if (contract == null)
            {
                return NotFound();
            }

            using MemoryStream output = contract.BuildContractWithSignature();

            return File(output.ToArray(), "application/pdf", $"{HttpUtility.UrlEncode(contract.ContractNo)}.pdf");
        }

        [HttpGet]
        [Route("DownloadFootprints")]
        [Authorize]
        public async Task<IActionResult> DownloadFootprintsAsync(string token)
        {
            Contract? contract = await GetContractAsync(token, false);

            if (contract == null)
            {
                return NotFound();
            }

            var pdfDoc = await _customContractService.GetFootprintsPdfDocument(contract);

            return File(pdfDoc.Stream.ToArray(), "application/pdf", $"{HttpUtility.UrlEncode(contract.ContractNo)}_history.pdf");
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login([FromBody] DownloadLoginViewModel viewModel)
        {
            var login = new LoginHandler(this);

            if (!login.ProcessLogin(viewModel.PID, viewModel.Password, out string msg))
            {
                return Ok(new { result = false, message = msg });
            }

            return Ok(new { result = true, message = "登入成功" });
        }
    }
}
