using CommonLib.Core.Utility;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.DataEntity;
using ContractHome.Models.ViewModel;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;

namespace ContractHome.Helper
{
    public class LoginHandler
    {
        private ControllerBase controller;
        public LoginHandler(ControllerBase controller)
        {
            //
            // TODO: Add constructor logic here
            //
            this.controller = controller;
        }

        public string? RedirectToAsLoginSuccessfully { get; set; }

        public bool ProcessLogin(string pid, string password, out string msg)
        {
            FileLogger.Logger.Info($"LoginHandler.ProcessLogin: pid:{pid} password:{password}");

            bool auth = false;
            using UserProfileManager mgr = new();
            UserProfile user = mgr.GetUserProfileByPID(pid);
            if (user?.LoginFailedCount >= 3 || user?.IsEnabled == false)
            {
                msg = "帳號密碼有誤";
                return auth;
            }

            user = UserProfileFactory.LoginProfileCheck(pid, password, out int? loginFailedCount);
            if (user == null) 
            {
                msg = "帳號密碼有誤";
                return auth;
            }
            return processLoginUsingRole(out msg, user);
            //if (up != null)
            //{
            //   if (up.Profile.UserProfileStatus.CurrentLevel == (int)Naming.MemberStatusDefinition.Wait_For_Check)
            //   {
            //       up.CurrentSiteMenu = "WaitForCheckMenu.xml";
            //       msg = VirtualPathUtility.ToAbsolute("~/UserProfile/EditMySelf?forCheck=True");
            //   }
            //}
            //return auth;
        }

        public bool ProcessLogin(string pid, out String msg)
        {
            UserProfile up = UserProfileFactory.CreateInstance(pid);
            return processLoginUsingRole(out msg, up);
        }


        private bool processLoginUsingRole(out string? msg, UserProfile up)
        {
            msg = null;
            bool bAuth = false;
            if (up != null)	//new login
            {
                var task = controller.HttpContext.SignOnAsync(up);
                task.Wait();
                bAuth = true;
            }

            if (bAuth)
            {
                string url = controller.Request.Query["ReturnUrl"];

                if (url != null && url.Length > 0 && !url.EndsWith("default.aspx"))
                {
                    //System.Web.Security.FormsAuthentication.RedirectFromLoginPage(up.PID, false);
                    msg = url;
                }
                else
                {
                    if (up.RoleIndex >= 0)
                    {
                        if (String.IsNullOrEmpty(RedirectToAsLoginSuccessfully))
                        {
                            //HttpContext.Current.Response.Redirect("MainPage.aspx", true);
                            msg = "~/Home/MainPage";
                        }
                        else
                        {
                            //HttpContext.Current.Response.Redirect(RedirectToAsLoginSuccessfully, true);
                            msg = RedirectToAsLoginSuccessfully;
                        }
                    }
                    else
                    {
                        bAuth = false;
                        //msg = "User role has not been approved!!";
                        msg = "使用者角色尚未被核定!!";
                    }
                }
            }
            else
            {
                //msg = "We could not find your information. Please sign in again";
                msg = "系統找不到您的資料，請重新登入!!";
            }

            return bAuth;
        }
    }

}
