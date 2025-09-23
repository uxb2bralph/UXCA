using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Hubs;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ContractHome.ApiControllers
{
    /// <summary>
    /// 快意簽Callback API
    /// </summary>
    /// <param name="hubContext"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class GRAController(IHubContext<SignatureHub> hubContext, DCDataContext db) : ControllerBase
    {
        private readonly IHubContext<SignatureHub> _hubContext = hubContext;

        private readonly DCDataContext _db = db;

        /// <summary>
        /// 使用者授權通知
        /// </summary>
        /// <param name="authNotify"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("AuthNotify")]
        public async Task<IActionResult> AuthNotify([FromBody] AuthNotify authNotify)
        {
            var (oneTimeToken, signature) = CHTSigningService.GetApplicationResponse();

            if (string.IsNullOrEmpty(authNotify.tid))
            {
                FileLogger.Logger.Info($"SignatureHub AuthNotify: No Has tid for result:{authNotify.result} resultMessage:{authNotify.resultMessage} email:{authNotify.email} expDate:{authNotify.expDate} tid:{authNotify.tid}");

                return Ok(new { oneTimeToken, signature });
            }

            var csr = (from c in _db.ContractSignatureRequest
                       where c.RequestTicket == authNotify.tid
                       select c).FirstOrDefault();

            if (csr == null)
            {
                FileLogger.Logger.Info($"SignatureHub AuthNotify: No matching ContractSignatureRequest found for tid:{authNotify.tid}");
                return Ok(new { oneTimeToken, signature });
            }

            string key = csr.ContractID.ToString();
            // 發送通知
            await _hubContext.Clients.Group(key)
                  .SendAsync("ReceiveUpdateNotice", key, authNotify.result, authNotify.resultMessage);

            FileLogger.Logger.Info($"SignatureHub AuthNotify: GroupKey:{key} result:{authNotify.result} resultMessage:{authNotify.resultMessage} email:{authNotify.email} expDate:{authNotify.expDate} tid:{authNotify.tid}");

            return Ok(new { oneTimeToken, signature });
        }

        /// <summary>
        /// 憑證申請完成通知
        /// </summary>
        /// <param name="certApply"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("CertApply")]
        public IActionResult CertApply([FromBody] CertApply certApply)
        {
            if (!string.IsNullOrEmpty(certApply.certSerial) && !string.IsNullOrEmpty(certApply.email))
            {
                var user = _db.UserProfile
                           .FirstOrDefault(u => u.EMail == certApply.email);

                if (user != null)
                {
                    user.ContactTitle = certApply.certSerial;
                    _db.SubmitChanges();
                }
            }

            FileLogger.Logger.Info($"SignatureHub CertApply: email:{certApply.email} resultMessage:{certApply.discountCode} whenCreated:{certApply.whenCreated} certSerial:{certApply.certSerial}");

            var (oneTimeToken, signature) = CHTSigningService.GetApplicationResponse();
            return Ok(new { oneTimeToken, signature });
        }
    }
}
