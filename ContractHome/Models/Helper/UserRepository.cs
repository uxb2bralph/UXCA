using CommonLib.Core.Utility;
using CommonLib.DataAccess;
using CommonLib.Utility;
using ContractHome.Helper.Security.MembershipManagement;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Office.CustomUI;
using Newtonsoft.Json;

namespace ContractHome.Models.Helper
{
    public class UserRepository
    {
        protected GenericManager<DCDataContext> _models;
        protected UserProfile? _userProfile;
        protected Organization? _organization;
        public UserRepository(GenericManager<DCDataContext> models, string pid) 
        {
            _models = models;
            UserProfileManager userProfile = new UserProfileManager();
            _userProfile = userProfile.GetUserProfileByPID(pid);
            _organization = _userProfile.OrganizationUser.Organization;
        }

        public UserProfile? UserProfile => _userProfile;
        public bool CanCreateContract => _organization?.CanCreateContract??false;
        public UserProfile? SaveContract()
        {
            _models.SubmitChanges();
            _models.Dispose();
            return _userProfile;
        }
    }
}
