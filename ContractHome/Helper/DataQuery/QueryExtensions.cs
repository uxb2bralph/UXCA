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

        public static Organization? GetOrganization(this UserProfile profile)
        {
            using var db = new DCDataContext();

            var organization = (from u in db.UserProfile
                             join ou in db.OrganizationUser on u.UID equals ou.UID
                             join o in db.Organization on ou.CompanyID equals o.CompanyID
                             where u.UID == profile.UID
                             select o).FirstOrDefault();

            return organization;
        }

        public static List<int> GetCategoryPermission(this UserProfile profile)
        {
            using var db = new DCDataContext();
            var categoryPermissions = (from u in db.UserProfile
                                       join c in db.ContractCategoryPermission on u.UID equals c.UID
                                       where u.UID == profile.UID
                                       select c.ContractCategoryID).ToList();
            return categoryPermissions.ToList();
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
