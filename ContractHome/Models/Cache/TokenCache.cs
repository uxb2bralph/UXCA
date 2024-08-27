using DocumentFormat.OpenXml.Spreadsheet;

namespace ContractHome.Models.Cache
{
    public class TokenCache : ICacheKey<Token>
    {
        private string _token;
        public TokenCache(string token)
        {
            _token = token;
        }

        public string CacheKey => $"{this.GetType().Name}_{this._token}";
    }

    public class Token
    {
    }
}
