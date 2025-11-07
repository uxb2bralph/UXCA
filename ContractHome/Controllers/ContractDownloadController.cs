using CommonLib.Core.Utility;
using ContractHome.Helper;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Dto;
using ContractHome.Models.Helper;
using Microsoft.AspNetCore.Mvc;
using static ContractHome.Helper.JwtTokenGenerator;

namespace ContractHome.Controllers
{
    public class ContractDownloadController(DCDataContext db, ContractServices contractServices) : Controller
    {
        private readonly DCDataContext _db = db;
        private readonly ContractServices _contractServices = contractServices;

        public async Task<IActionResult> IndexAsync(string type, string token)
        {
            var profile = await HttpContext.GetUserAsync();

            if (string.IsNullOrEmpty(token) || !IsValidToken(token, profile))
            {
                return RedirectToAction("Login", "Account");
            }

            if (profile != null)
            {
                string action = (type == "footprints") ? "DownloadFootprints" : "DownloadContract";
                return Redirect($"api/ContractDownload/{action}?token={Uri.EscapeDataString(token)}");
            }

            return View();
        }

        private bool IsValidToken(string token, UserProfile? profile)
        {
            try
            {
                (BaseResponse resp, JwtToken jwtTokenObj, UserProfile userProfile)
                        = _contractServices.TokenDownloadValidate(JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData());
                if (resp.HasError)
                {
                    return false;
                }
                if (string.IsNullOrEmpty(jwtTokenObj.ContractID))
                {
                    return false;
                }

                var contract = _db.GetTable<Contract>()
                                .Where(c => c.ContractID == jwtTokenObj.ContractID.DecryptKeyValue())
                                .FirstOrDefault();

                if (contract == null)
                {
                    return false;
                }

                if (profile != null && profile?.UID != userProfile.UID)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                var tokenSanitized = token.Replace("\r", "").Replace("\n", "");
                FileLogger.Logger.Error($"TokenValidate failed. JwtToken={tokenSanitized};   {ex}");
                return false;
            }
        }
    }
}
