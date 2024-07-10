namespace ContractHome.Models.Cache
{
    public interface ICacheStore
    {
        void Add<TItem>(TItem item);

        //TItem Get<TItem>(ICacheKey<TItem> key) where TItem : class;
        TItem Get<TItem>(TItem key) where TItem : class;

    }
}
