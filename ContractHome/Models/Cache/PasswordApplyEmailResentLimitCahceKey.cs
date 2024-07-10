using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class PasswordApplyEmailResentLimitCahceKey : ICacheKey
    {
        private string _email;
        public PasswordApplyEmailResentLimitCahceKey()
        {

        }

        public string CreateCacheKey(string email)
        {
            _email = email;
            return $"PasswordApplyUsedToken_{this._email}";
        }
    }
}
