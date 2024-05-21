namespace ContractHome.Models.Cache
{
    internal class TrustAffixPdfSealTokenCahceKey : ICacheKey<Default>
    {
        private string _token;

        public TrustAffixPdfSealTokenCahceKey(string token)
        {
            this._token = token;
        }

        public string CacheKey => $"{this.GetType().Name}_{this._token}";

        public string DurationSetting => "Default";
    }
}