using CommonLib.DataAccess;
using ContractHome.Models.DataEntity;

namespace ContractHome.Helper.DataQuery
{
    public static class QueryExtensions
    {
        public static UserProfile LoadInstance(this UserProfile profile, GenericManager<DCDataContext> models)
        {
            return models.GetTable<UserProfile>().Where(u => u.UID == profile.UID).First();
        }

    }
}
