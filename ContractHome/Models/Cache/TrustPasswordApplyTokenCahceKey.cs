using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class TrustPasswordApplyTokenCahceKey : ICacheKey<Default>
    {
        private readonly string _token;
        public TrustPasswordApplyTokenCahceKey(string token)
        {
            _token = token;
        }

        public string CacheKey => $"{this.GetType().Name}_{this._token}";

        public string DurationSetting => "Default";
    }
}
