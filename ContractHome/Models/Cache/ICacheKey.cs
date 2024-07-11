namespace ContractHome.Models.Cache
{
    public interface ICacheKey
    {
        void CreateCacheKey(string keyID);
        string CacheKey { get; }
    }
}
