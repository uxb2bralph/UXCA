﻿namespace ContractHome.Models.Cache
{
    public interface ICacheStore
    {
        void Add<TItem>(TItem item, ICacheKey<TItem> key);

        TItem Get<TItem>(ICacheKey<TItem> key) where TItem : class;

    }
}
