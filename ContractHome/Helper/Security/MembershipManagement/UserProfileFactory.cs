using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using System.Security.Cryptography.X509Certificates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ContractHome.Helper.Security.MembershipManagement
{
    public static class UserProfileFactory
    {
        internal static string PasswordRegex => @"^(?=.*\d)(?=.*[a-zA-Z])(?=.*\W).{8,30}$";
        public static UserProfile? CreateInstance(int uid)
        {
            using UserProfileManager mgr = new UserProfileManager();
            return mgr.GetUserProfile(uid);
        }

        public static bool CompareEncryptedPassword(
            string clearPassword, 
            string encryptedPassword)
        {
            var ttt = clearPassword.HashPassword();
            return (string.Compare(clearPassword.HashPassword(), encryptedPassword, true) == 0);
        }

        public static bool VerifyPassword(UserProfile profile, string password)
        {
            bool result = false;
            if ((profile == null) || (string.IsNullOrEmpty(password))) { return result; }
            CipherDecipherSrv cipher = new CipherDecipherSrv(10);
            if (password.Equals(cipher.decipher(profile.Password)))
            {
                result = true;
            }
            else if (CompareEncryptedPassword(password, profile.Password2))
            {
                result = true;
            }
            return result;
        }


        public static UserProfile? LoginProfileCheck(string pid, string password, out int? loginFailedCount)
        {
            //UserProfile? profile = CreateInstance(pid);
            using UserProfileManager mgr = new();
            UserProfile profile = mgr.GetUserProfileByPID(pid);

            if (profile == null)
            {
                loginFailedCount = 0;
                return null;
            }

            loginFailedCount = 0;
            if (profile?.LoginFailedCount>=3)
            {
                loginFailedCount = profile.LoginFailedCount;
                profile = null;
            }
            else if (profile != null && VerifyPassword(profile, password))
            {
                profile.Password = password.HashPassword();
                profile.LoginFailedCount = loginFailedCount;
            }
            else
            {
                profile.LoginFailedCount = profile.LoginFailedCount+1;
                loginFailedCount = profile.LoginFailedCount;
                profile = null;
            }

            mgr.SubmitChanges();
            return profile;
        }

        public static UserProfile? CreateInstance(string pid)
        {
            using UserProfileManager mgr = new();
            return mgr.GetUserProfileByPID(pid);
        }
    }

}
