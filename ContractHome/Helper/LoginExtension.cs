using CommonLib.Utility;
using ContractHome.Helper.DataQuery;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.DataEntity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace ContractHome.Helper
{
    public static class LoginExtension
    {

        public static async Task SignOnAsync(this HttpContext context, UserProfile profile, bool remeberMe = true)
        {
            //帳密都輸入正確，ASP.net Core要多寫三行程式碼
            bool isSysAdmin = profile.IsSysAdmin();
            Claim[] claims = new[] {
                new Claim("Name", profile.PID),
                new Claim("UID", profile.UID.ToString()),
                new Claim("IsAdmin", isSysAdmin.ToString()),
                new Claim("RoleIDs", profile.GetRoleIDs())
            }; //Key取名"Name"，在登入後的頁面，讀取登入者的帳號會用得到，自己先記在大腦
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);//Scheme必填
            ClaimsPrincipal principal = new ClaimsPrincipal(claimsIdentity);

            //從組態讀取登入逾時設定
            double loginExpireMinute = Properties.Settings.Default.LoginExpireMinutes;
            //執行登入，相當於以前的FormsAuthentication.SetAuthCookie()
            await context.SignInAsync(principal,
                new AuthenticationProperties()
                {
                    IsPersistent = true, //IsPersistent = false：瀏覽器關閉立馬登出；IsPersistent = true 就變成常見的Remember Me功能
                                         //用戶頁面停留太久，逾期時間，在此設定的話會覆蓋Startup.cs裡的逾期設定
                    ExpiresUtc = DateTime.UtcNow.AddMinutes(loginExpireMinute)
                });

            context.User = principal;

            string enPid = profile.PID.EncryptData();

            var organization = profile.GetOrganization();

            profile.UserCompanyID = organization.CompanyID;
            profile.CategoryPermission = profile.GetCategoryPermission();

            int roleID = profile.GetUserRole().RoleID;

            profile.IsSysAdmin = roleID == (int)UserRoleDefinition.RoleEnum.SystemAdmin;
            profile.IsMemberAdmin = roleID == (int)UserRoleDefinition.RoleEnum.MemberAdmin;

            profile.UserCompanyName = organization.CompanyName;
            profile.UserCompanyReceiptNo = organization.ReceiptNo;

            context.Response.Cookies.Append("userID", enPid,
            new CookieOptions
            {
                MaxAge = TimeSpan.FromHours(24),
            });

            //context.ClearCache();
            context.RemoveCache($"{enPid}-userProfile");
            context.SetCacheValue($"{enPid}-userProfile", profile);

            /// process sign-on user profile
            /// 
            var roles = profile.UserRole.Select(r => r.UserRoleDefinition).ToArray();
        }


        public static void ClearCache(this HttpContext context)
        {
            HttpContextDataModelCache caching = new(context);
            caching.Clear();
        }


        public static void RemoveCache(this HttpContext context, String keyName)
        {
            HttpContextDataModelCache caching = new(context);
            caching.Remove(keyName);
        }


        public static Object GetCacheValue(this HttpContext context, String keyName)
        {
            HttpContextDataModelCache caching = new(context);
            return caching[keyName];
        }

        public static void SetCacheValue(this HttpContext context, String keyName, Object value)
        {
            HttpContextDataModelCache caching = new(context);
            caching[keyName] = value;
        }

        public static async void Logout(this HttpContext context)
        {
            var pid = context.Request.Cookies["userID"];
            if (!String.IsNullOrEmpty(pid))
            {
                context.RemoveCache($"{pid}-userProfile");
            }

            context.Response.Cookies.Delete("userID");
            await context.SignOutAsync();
        }

        public static async void TrustLogout(this HttpContext context)
        {
            var pid = context.Request.Cookies["userID"];
            if (!String.IsNullOrEmpty(pid))
            {
                context.RemoveCache($"{pid}-userProfile");
            }

            context.Response.Cookies.Delete("userID");
            await context.SignOutAsync();
        }

        public static UserProfile GetUser(this HttpContext context)
        {
            var result = context.GetUserAsync();
            result.Wait();
            return result.Result;
        }

        public static async Task<UserProfile> GetUserAsync(this HttpContext context)
        {
            var pid = context.Request.Cookies["userID"]?.ToString() ?? string.Empty;
            
            if (string.IsNullOrEmpty(pid) && !context.User.Identity.IsAuthenticated)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(pid))
            {
                UserProfile profile = (UserProfile)context.GetCacheValue($"{pid}-userProfile");

                if (profile != null)
                {
                    return profile;
                }
            }

            if (context.User.Identity.IsAuthenticated)
            {
                UserProfile profile = (context.User.Identity as ClaimsIdentity)?
                    .Claims.FirstOrDefault()?.Value.getLoginUser();
                await context.SignOnAsync(profile);
                return profile;
            }

            return null;
        }

        private static UserProfile? getLoginUser(this String pid)
        {
            using(UserProfileManager models = new UserProfileManager())
            {
                return models.GetUserProfileByPID(pid);
            }
        }

        public static bool IsSysAdmin(this UserProfile profile)
        {
            return profile != null && (profile.UserRole.Any(r => r.RoleID == (int)UserRoleDefinition.RoleEnum.SystemAdmin));
        }

        public static bool IsMemberAdmin(this UserProfile profile)
        {
            return profile != null && (profile.UserRole.Any(r => r.RoleID == (int)UserRoleDefinition.RoleEnum.MemberAdmin));
        }

        public static bool IsAuthorized(this UserProfile profile, params int[] roleID)
        {
            return profile != null && profile.UserRole.Join(roleID, r => r.RoleID, o => o, (r, o) => r).Any();
        }

        public static bool IsAuthorized(this UserProfile profile, string userRoleIDs, int[] checkRoleIDs)
        {
            if (profile == null || string.IsNullOrEmpty(userRoleIDs) || checkRoleIDs == null || checkRoleIDs.Length == 0)
                return false;

            var roleIDs = userRoleIDs.Split(',').Select(int.Parse).ToArray();
            var roleSet = new HashSet<int>(roleIDs);
            bool isAuthorized = checkRoleIDs.Any(roleID => roleSet.Contains(roleID));
            return isAuthorized;
        }

        public static bool IsUser(this UserProfile profile)
        {
            return profile != null && (profile.UserRole.Any(r => r.RoleID == (int)UserRoleDefinition.RoleEnum.User));
        }

        public static string GetRoleIDs(this UserProfile profile)
        {
            if (profile == null) return string.Empty;
            return string.Join(",", profile.UserRole.OrderBy(c => c.RoleID).Select(r => r.RoleID));
        }

        public static bool CanCreateContract(this UserProfile profile)
        {
            if (profile?.OrganizationUser == null) return false;
            if (profile?.OrganizationUser.Organization == null) return false;
            if (profile?.OrganizationUser.Organization.CanCreateContract==true) return true;
            return false;
        }
    }
}