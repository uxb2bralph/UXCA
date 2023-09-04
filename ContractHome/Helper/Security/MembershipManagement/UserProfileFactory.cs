using CommonLib.Utility;
using ContractHome.Models.DataEntity;
using System.Security.Cryptography.X509Certificates;

namespace ContractHome.Helper.Security.MembershipManagement
{
    public static class UserProfileFactory
    {

        public static UserProfile? CreateInstance(int uid)
        {
            using UserProfileManager mgr = new UserProfileManager();
            return mgr.GetUserProfile(uid);
        }

        public static UserProfile? CreateInstance(string pid, string password)
        {
            UserProfile? profile = CreateInstance(pid);
            if (profile != null)
            {
                CipherDecipherSrv cipher = new CipherDecipherSrv(10);
                if (!String.IsNullOrEmpty(profile.Password) && password.Equals(cipher.decipher(profile.Password)))
                {
                    profile.Password = password;
                    return profile;
                }
                else if (String.Compare(password.HashPassword(), profile.Password2, true) == 0)
                {
                    profile.Password = password;
                    return profile;
                }
            }

            return null;
        }

        public static UserProfile? CreateInstance(string pid)
        {
            using UserProfileManager mgr = new();
            return mgr.GetUserProfileByPID(pid);
        }
    }

}
