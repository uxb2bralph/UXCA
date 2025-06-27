using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Hubs;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ContractHome.ApiControllers
{
    /// <summary>
    /// 快意簽Callback API
    /// </summary>
    /// <param name="hubContext"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class GRAController(IHubContext<SignatureHub> hubContext) : ControllerBase
    {
        private readonly IHubContext<SignatureHub> _hubContext = hubContext;

        /// <summary>
        /// 使用者授權通知
        /// </summary>
        /// <param name="authNotify"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("AuthNotify")]
        public async Task<IActionResult> AuthNotify([FromBody] AuthNotify authNotify)
        {
            if (!string.IsNullOrEmpty(authNotify.tid) && !string.IsNullOrEmpty(authNotify.email))
            {
                using var db = new DCDataContext();

                var csr = (from c in db.ContractSignatureRequest
                           where c.RequestTicket == authNotify.tid
                           select c).FirstOrDefault();

                var user = (from u in db.UserProfile
                            where u.EMail == authNotify.email
                            select u).FirstOrDefault();

                if (csr != null && user != null)
                {
                    string key = user.PID + "_" + csr.ContractID;
                    // 發送通知
                    await _hubContext.Clients.Group(key)
                          .SendAsync("ReceiveUpdateNotice", key, authNotify.result, authNotify.resultMessage);
                }
            }

            FileLogger.Logger.Info($"AuthNotify: result:{authNotify.result} resultMessage:{authNotify.resultMessage} email:{authNotify.email} expDate:{authNotify.expDate} tid:{authNotify.tid}");

            var (oneTimeToken, signature) = CHTSigningService.GetApplicationResponse();
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
                using var db = new DCDataContext();

                var user = db.UserProfile
                           .FirstOrDefault(u => u.EMail == certApply.email);

                if (user != null)
                {
                    user.ContactTitle = certApply.certSerial;
                    db.SubmitChanges();
                }
            }

            FileLogger.Logger.Info($"CertApply: email:{certApply.email} resultMessage:{certApply.discountCode} whenCreated:{certApply.whenCreated} certSerial:{certApply.certSerial}");

            var (oneTimeToken, signature) = CHTSigningService.GetApplicationResponse();
            return Ok(new { oneTimeToken, signature });
        }
    }
}
