namespace ContractHome.Models.Cache
{
    public interface ICacheKey
    {
        string CreateCacheKey(string keyID);
        //string DurationSetting { get; }
    }
}
