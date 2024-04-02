using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Helper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using System.Diagnostics.Contracts;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Controllers
{
    public class IdentityCertController : SampleController
    {
        private readonly ILogger<IdentityCertController> _logger;
        //private readonly IMailService _mailService;
        //private readonly EmailBody _emailBody;
        //private readonly EmailFactory _emailFactory;
        //private readonly IMemoryCache _memCache;
        private static readonly int tokenTTLMins = 10;
        private static readonly int reSendEmailMins = 3;
        private IdentityCertRepo identityCertRepo;
        private IdentityCertHelper identityCertHelper;
        private static string begin = "-----BEGIN CERTIFICATE-----";
        private static string end = "-----END CERTIFICATE-----";

        public IdentityCertController(ILogger<IdentityCertController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
            //_mailService = ServiceProvider.GetRequiredService<IMailService>();
            //_emailFactory = serviceProvider.GetRequiredService<EmailFactory>();
            //_emailBody = serviceProvider.GetRequiredService<EmailBody>();
            //_memCache = serviceProvider.GetRequiredService<IMemoryCache>();
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] PostIdentityCertRequest req)
        {
            if (!ModelState.IsValid) { return BadRequest(); }
            var profile = await HttpContext.GetUserAsync();
            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            //}
            //#endregion
            if (profile==null)
            {
                ModelState.AddModelError("eUID", "身份驗證失敗");
            }


            identityCertHelper = new($"{begin}{req.B64Cert}{end}");
            if (identityCertHelper==null)
            {
                ModelState.AddModelError("b64Cert", "格式有誤");
            }

            if (ModelState.IsValid)
            {
                if (!identityCertHelper.IsSignatureValid(profile.PID, req.Signature))
                {
                    ModelState.AddModelError("signature", "驗章失敗");
                }
            }

            if (!ModelState.IsValid) { return BadRequest(); }

                identityCertRepo = new(models);
            var existedIdentityCert = identityCertRepo.Get(
                x509String: identityCertHelper.X509PemString
                , uid: profile.UID).FirstOrDefault();
            if (existedIdentityCert != null)
            {
                ModelState.AddModelError("b64Cert", "憑證已註冊");
            }

            if (ModelState.IsValid)
            {
                IdentityCert identityCert = new();
                identityCert.X509Certificate = identityCertHelper.X509PemString;
                identityCert.BindingUID = profile.UID;
                identityCert.CertificateType = identityCertHelper.GetCertType();
                identityCertRepo.AddSubmitChanges(identityCert);

                return Json(new BaseResponse(data: new { eSeqNo = identityCert.SeqNo.EncryptKey() }));
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<ActionResult> Validate([FromBody] ValidateIdentityCertRequest req)
        {
            if (!ModelState.IsValid) { return BadRequest(); }
            var profile = await HttpContext.GetUserAsync();
            //#region add for postman test
            //if (profile == null)
            //{
            //    profile = models.GetTable<UserProfile>().Where(x => x.UID == 4).FirstOrDefault();
            //}
            //#endregion
            if (profile == null)
            {
                ModelState.AddModelError("eUID", "身份驗證失敗");
                return BadRequest();
            }

            identityCertRepo = new(models);
            var identityCert = identityCertRepo.GetByUid(profile.UID).FirstOrDefault();
            if (identityCert == null)
            {
                ModelState.AddModelError("identityCert", "使用者未註冊憑證");
                return BadRequest();
            }
            var existedIdentityCert = identityCertRepo.Get(
                x509String: identityCert.X509Certificate
                , uid: profile.UID).FirstOrDefault();

            identityCertHelper = new(x509PemString: existedIdentityCert.X509Certificate);
            if (!identityCertHelper.IsSignatureValid(profile.PID, req.Signature))
            {
                ModelState.AddModelError("signature", "驗章失敗");
            }

            if (ModelState.IsValid) 
            { return Ok(new BaseResponse()); }
            else
            { return BadRequest(); }

        }

        [HttpPost]
        public async Task<ActionResult> Revoke([FromBody] RevokeIdentityCertRequest req)
        {
            if (!ModelState.IsValid) { return BadRequest(); }
            identityCertRepo = new(models);
            var existedIdentityCert = identityCertRepo.GetBySeqNo(seqNo:req.ESeqNo.DecryptKeyValue());
            if (existedIdentityCert == null)
            {
                ModelState.AddModelError("seqNo", "資料錯誤");
            }

            if (!ModelState.IsValid) { return BadRequest(); }

            identityCertRepo.DeleteSubmitChanges(seqNo: req.ESeqNo.DecryptKeyValue());
            return Json(new BaseResponse());
        }

    }
}