using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Office.CustomUI;
using Newtonsoft.Json;

namespace ContractHome.Models.Helper
{
    public class UserProfileRepository
    {
        protected internal GenericManager<DCDataContext> _models;
        protected internal UserProfile? _userProfile;
        public UserProfileRepository(GenericManager<DCDataContext> models, string pid) 
        {
            _models = models;
            UserProfileManager userProfile = new UserProfileManager();
            _userProfile = userProfile.GetUserProfileByPID(pid);
        }

        public UserProfile? UserProfile => _userProfile;

        public UserProfile? SaveContract()
        {
            _models.SubmitChanges();
            _models.Dispose();
            return _userProfile;
        }
    }
}
