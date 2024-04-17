using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class PasswordApplyUsedTokenCahceKey : ICacheKey<Default>
    {
        private readonly string _token;
        public PasswordApplyUsedTokenCahceKey(string token)
        {
            _token = token;
        }

        public string CacheKey => $"PasswordApplyUsedToken_{this._token}";

        public string DurationSetting => "Default";
    }
}
