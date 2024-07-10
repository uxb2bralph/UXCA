using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class TrustPasswordApplyTokenCahceKey : ICacheKey
    //public class TrustPasswordApplyTokenCahceKey
    {
        private string _token;
        public TrustPasswordApplyTokenCahceKey()
        {

        }

        public string CreateCacheKey(string token)
        {
            _token = token;
            return $"PasswordApplyUsedToken_{this._token}";
        }

        //public string DurationSetting => "Default";
    }
}
