namespace ContractHome.Models.Cache
{
    public interface ICacheKey<TItem>
    {
        string CacheKey { get; }
        string DurationSetting { get; }
    }
}
