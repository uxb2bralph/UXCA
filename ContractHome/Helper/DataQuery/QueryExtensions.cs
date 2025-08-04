using CommonLib.DataAccess;
using ContractHome.Models.DataEntity;
using Microsoft.Identity.Client;

namespace ContractHome.Helper.DataQuery
{
    public static class QueryExtensions
    {
        public static UserProfile LoadInstance(this UserProfile profile, GenericManager<DCDataContext> models)
        {
            return models.GetTable<UserProfile>().Where(u => u.UID == profile.UID).First();
        }

        public static int GetCompanyID(this UserProfile profile)
        {
            using var db = new DCDataContext();

            var companyID = (from u in db.UserProfile
                         join o in db.OrganizationUser on u.UID equals o.UID
                         where u.UID == profile.UID
                         select o.CompanyID).FirstOrDefault();

            return companyID;
        }

        public static UserRole GetUserRole(this UserProfile profile)
        {
            using var db = new DCDataContext();
            var userRole = db.UserRole.FirstOrDefault(r => r.UID == profile.UID);
            if (userRole != null)
            {
                userRole.UserRoleDefinition = db.UserRoleDefinition.FirstOrDefault(rd => rd.RoleID == userRole.RoleID);
            }
            return userRole;
        }

    }
}
