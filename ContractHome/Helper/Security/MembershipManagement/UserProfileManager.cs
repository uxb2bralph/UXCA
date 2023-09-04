using CommonLib.DataAccess;
using ContractHome.Models.DataEntity;
using DocumentFormat.OpenXml.Presentation;
using System.Data.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ContractHome.Helper.Security.MembershipManagement
{
    public partial class UserProfileManager : GenericManager<DCDataContext>
    {
        public UserProfileManager() : base() { }
        public UserProfileManager(GenericManager<DCDataContext> mgr) : base(mgr) { }

        public UserProfile? GetUserProfile(int uid)
        {
            DataLoadOptions ops = new DataLoadOptions();
            ops.LoadWith<UserProfile>(u => u.UserRole);
            ops.LoadWith<UserRole>(r => r.UserRoleDefinition);

            _db.LoadOptions = ops;

            var item = _db.UserProfile.Where(u => u.UID == uid).FirstOrDefault();
            item?.DetermineUserRole();
            return item;
        }

        public UserProfile? GetUserProfileByPID(string pid)
        {
            DataLoadOptions ops = new DataLoadOptions();
            ops.LoadWith<UserProfile>(u => u.UserRole);
            ops.LoadWith<UserRole>(r => r.UserRoleDefinition);

            _db.LoadOptions = ops;

            var item = _db.UserProfile.Where(u => u.PID == pid/* & u.UserProfileStatus.CurrentLevel != (int)Naming.MemberStatusDefinition.Mark_To_Delete*/).FirstOrDefault();
            item?.DetermineUserRole();
            return item;
        }


        public IQueryable<UserProfile> GetUserByUserRole(IQueryable<UserProfile> items, int roleID)
        {
            return items.Join(GetTable<UserRole>().Where(r => r.RoleID == roleID), u => u.UID, r => r.UID, (u, r) => u);
        }



    }

}
