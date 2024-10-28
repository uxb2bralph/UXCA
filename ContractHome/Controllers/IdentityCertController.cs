using CommonLib.Utility;
using ContractHome.Helper;
using ContractHome.Helper.DataQuery;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Helper;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using System.Diagnostics.Contracts;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Controllers
{
    public class IdentityCertController : SampleController
    {
        private readonly ILogger<IdentityCertController> _logger;
        private IdentityCertRepo identityCertRepo;
        private IdentityCertHelper identityCertHelper;
        BaseResponse defaultResponse = new BaseResponse();

        public IdentityCertController(ILogger<IdentityCertController> logger, IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
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
                ModelState.AddModelError("EUID", "身份驗證失敗");
            }


            identityCertHelper = new(req.B64Cert);
            if (identityCertHelper==null)
            {
                ModelState.AddModelError("B64Cert", "格式有誤");
            }

            if (ModelState.IsValid)
            {
                if (!identityCertHelper.IsSignatureValid(profile.PID, req.Signature))
                {
                    ModelState.AddModelError("Signature", "驗章失敗");
                }
            }

            if (!ModelState.IsValid) { return BadRequest(); }

                identityCertRepo = new(models);
            var existedIdentityCert = identityCertRepo.Get(
                x509String: identityCertHelper.X509PemString
                , uid: profile.UID).FirstOrDefault();
            if (existedIdentityCert != null)
            {
                ModelState.AddModelError("B64Cert", "憑證已註冊");
            }

            if (ModelState.IsValid)
            {
                IdentityCert identityCert = new();
                identityCert.X509Certificate = identityCertHelper.X509PemString;
                identityCert.BindingUID = profile.UID;
                identityCert.CertificateType = identityCertHelper.GetCertType();
                identityCertRepo.AddSubmitChanges(identityCert);

                var resp = new BaseResponse(data: new { ESeqNo = identityCert.SeqNo.EncryptKey() });
                return Ok(resp);
            }

            return BadRequest();
        }

        [HttpPost]
        public async Task<ActionResult> ValidateBySubject([FromBody] ValidateIdentityCertRequest req)
        {
            if (!ModelState.IsValid) { return BadRequest(); }

            //#region add for postman test
            try
            {
                var profile = (await HttpContext.GetUserAsync()).LoadInstance(models);
                //var profile = models.GetTable<UserProfile>().Where(x => x.UID == 11).FirstOrDefault();
                //#endregion
                if (profile == null)
                {
                    ModelState.AddModelError("EUID", "身份驗證失敗");
                    return BadRequest();
                }

                identityCertHelper = new(x509PemString: req.B64Cert);
                //if (!identityCertHelper.IsSubjectMatch(profile.CompanyName))
                //{
                //    ModelState.AddModelError("CompanyName", "憑證資料不符(O)");
                //}
                //if (identityCertHelper.IsCorporateCert
                //    &&
                //    !identityCertHelper.IsSubjectMatch(profile.Organization.ReceiptNo))
                if (profile.IsUserWantToCheckReceiptNo)
                {
                    if (identityCertHelper.IsCorporateCert
                        &&
                        !identityCertHelper.IsSubjectMatch(profile.GetReceiptNoByRole))
                    {
                        ModelState.AddModelError("ReceiptNo", "憑證資料不符(ReceiptNo)");
                    }
                }

                if (!identityCertHelper.IsSignatureValid(profile.PID, req.Signature))
                {
                    ModelState.AddModelError("Signature", "驗章失敗");
                }

                if (ModelState.IsValid)
                { return Json(defaultResponse); }
                else
                { return BadRequest(); }
            }
            catch (Exception ex)
            {
                defaultResponse = new BaseResponse(haserror:true, error:"系統錯誤");
                return View("~/Views/Shared/CustomMessage.cshtml", defaultResponse);
            }


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
                ModelState.AddModelError("EUID", "身份驗證失敗");
                return BadRequest();
            }

            identityCertRepo = new(models);
            var identityCert = identityCertRepo.GetByUid(profile.UID).FirstOrDefault();
            if (identityCert == null)
            {
                ModelState.AddModelError("IdentityCert", "使用者未註冊憑證");
                return BadRequest();
            }
            var existedIdentityCert = identityCertRepo.Get(
                x509String: identityCert.X509Certificate
                , uid: profile.UID).FirstOrDefault();

            identityCertHelper = new(x509PemString: existedIdentityCert.X509Certificate);
            if (!identityCertHelper.IsSignatureValid(profile.PID, req.Signature))
            {
                ModelState.AddModelError("Signature", "驗章失敗");
            }

            if (ModelState.IsValid) 
            { return Content(defaultResponse.JsonStringify()); }
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
                ModelState.AddModelError("SeqNo", "資料錯誤");
            }

            if (!ModelState.IsValid) { return BadRequest(); }

            identityCertRepo.DeleteSubmitChanges(seqNo: req.ESeqNo.DecryptKeyValue());
            return Content(defaultResponse.JsonStringify());
        }

    }
}