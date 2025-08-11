using CommonLib.Core.Utility;
using ContractHome.Helper;
using FluentValidation;
using CommonLib.Utility;
using ContractHome.Models.DataEntity;

namespace ContractHome.Services.UserProfileManage
{
    public class PIDAndPasswordUpdateRequest : PIDAndPasswordUpdateModel
    {
        public class Validator : AbstractValidator<PIDAndPasswordUpdateRequest>
        {
            public Validator()
            {
                RuleLevelCascadeMode = CascadeMode.Stop;

                RuleFor(x => x.PID).NotEmpty().WithMessage("請輸入帳號")
                                    .Matches(@"^(?=.*[a-z]).{6,30}$")
                                    .WithMessage("帳號格式錯誤");

                RuleFor(x => x.PID).Must((x, pid) => ValidatePID(x.UID, pid)).WithMessage("帳號已被使用");

                RuleFor(x => x.NewPassword).NotEmpty().WithMessage("請輸入新密碼")
                                    .Matches(@"^(?=.*\d)(?=.*[!@#$%^&*])(?=.*[a-z]).{6,30}$")
                                    .WithMessage("新密碼格式錯誤");

                RuleFor(x => x.ReNewPassword)
                    .Equal(x => x.NewPassword)
                    .WithMessage("新密碼與確認新密碼輸入不一致");

                RuleFor(x => x.Token).Must(ValidateToken).WithMessage("Token驗證錯誤");
            }

            private bool ValidatePID(int uid, string pid) 
            {
                using var db = new DCDataContext();
                var userProfile = (from u in db.UserProfile
                                   where !u.UID.Equals(uid)
                                   where u.PID.Equals(pid)
                                   select u).FirstOrDefault();
                if (userProfile != null)
                {
                    return false;
                }
                return true;
            }

            private bool ValidateToken(string token)
            {
                try
                {
                    token = JwtTokenValidator.Base64UrlDecodeToString(token).DecryptData().GetEfficientString();

                    if (string.IsNullOrEmpty(token))
                    {
                        return false;
                    }

                    if (!JwtTokenValidator.ValidateJwtToken(token, JwtTokenGenerator.secretKey))
                    {
                        return false;
                    }
                    var jwtTokenObj = JwtTokenValidator.DecodeJwtToken(token);
                    if (jwtTokenObj == null)
                    {
                        return false;
                    }

                    using var db = new DCDataContext();

                    var userProfile = (from u in db.UserProfile
                                       where u.EMail.Equals(jwtTokenObj.Email)
                                       where u.UID.Equals(jwtTokenObj.UID.DecryptKeyValue())
                                       select u).FirstOrDefault();

                    if (userProfile == null)
                    {
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    FileLogger.Logger.Error($"UserProfilePIDAndPasswordUpdateRequest validateToken failed. JwtToken={token};   {ex}");
                    return false;
                }
            }

        }
    }
}
