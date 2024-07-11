using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class TokenCache : ICacheKey
    {
        private string _token;
        public TokenCache()
        {

        }

        public string CacheKey => $"TokenCahce_{this._token}";

        public void CreateCacheKey(string token)
        {
            _token = token;
        }
    }
}
