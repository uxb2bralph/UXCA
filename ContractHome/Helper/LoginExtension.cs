using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;  //System.Web.Mvc;
using Microsoft.Extensions.Logging;
using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ContractHome.Helper.Security.MembershipManagement;
using System.Data.Linq.SqlClient;

namespace ContractHome.Helper
{
    public static class LoginExtension
    {

        public static async Task SignOnAsync(this HttpContext context, UserProfile profile, bool remeberMe = true)
        {
            //帳密都輸入正確，ASP.net Core要多寫三行程式碼 
            Claim[] claims = new[] { new Claim("Name", profile.PID), new Claim("IsAdmin", profile.IsSysAdmin().ToString()) }; //Key取名"Name"，在登入後的頁面，讀取登入者的帳號會用得到，自己先記在大腦
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

            context.ClearCache();
            context.SetCacheValue("userProfile", profile);

            if (remeberMe)
            {
                context.Response.Cookies.Append("userID", profile.PID.EncryptData(),
                    new CookieOptions
                    {
                        MaxAge = TimeSpan.FromDays(14),
                    });
            }
            else
            {
                context.Response.Cookies.Append("userID", profile.PID.EncryptData(),
                    new CookieOptions
                    {
                        MaxAge = TimeSpan.FromHours(24),
                    });
            }


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
            context.SetCacheValue(keyName, null);
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
            context.Response.Cookies.Delete("userID");
            await context.SignOutAsync();
            context.ClearCache();
        }

        public static async void TrustLogout(this HttpContext context)
        {
            context.Response.Cookies.Delete("userID");
            await context.SignOutAsync();
            context.ClearCache();
        }

        public static UserProfile GetUser(this HttpContext context)
        {
            var result = context.GetUserAsync();
            result.Wait();
            return result.Result;
        }

        public static async Task<UserProfile> GetUserAsync(this HttpContext context)
        {
            UserProfile profile = (UserProfile)context.GetCacheValue("userProfile");
            //CommonLib.Core.Utility.FileLogger.Logger.Debug("profile cache:" + (profile != null));
            if (profile == null)
            {
                if (context.User.Identity.IsAuthenticated)
                {
                    //CommonLib.Core.Utility.FileLogger.Logger.Debug("Has Identity:" + context.User.Identity.Name);
                    profile = (context.User.Identity as ClaimsIdentity)?
                        .Claims.FirstOrDefault()?.Value.getLoginUser();
                }
                else
                {
                    var cookie = context.Request.Cookies["userID"];
                    if (!String.IsNullOrEmpty(cookie))
                    {
                        try
                        {
                            profile = cookie.DecryptData().getLoginUser();
                            if (profile != null)
                            {
                                await context.SignOnAsync(profile);
                            }
                        }
                        catch (Exception ex)
                        {
                            ApplicationLogging.LoggerFactory.CreateLogger(typeof(LoginExtension))
                                .LogError(ex, ex.Message);
                            profile = null;
                        }
                    }
                }
                context.SetCacheValue("userProfile", profile);
            }
            return profile;
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

        public static bool IsUser(this UserProfile profile)
        {
            return profile != null && (profile.UserRole.Any(r => r.RoleID == (int)UserRoleDefinition.RoleEnum.User));
        }
        public static bool IsOperator(this UserProfile profile)
        {
            return profile != null && (profile.UserRole.Any(r => r.RoleID == (int)UserRoleDefinition.RoleEnum.Operator));
        }
        public static bool CanCreateContract(this UserProfile profile)
        {
            if (profile.IsUser()) 
            {
                return true;
            }
            return false;
            //if (profile?.OrganizationUser == null) return false;
            //if (profile?.OrganizationUser.Organization == null) return false;
            //if (profile?.OrganizationUser.Organization.CanCreateContract==true) return true;
        }
    }
}