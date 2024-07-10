using DocumentFormat.OpenXml.InkML;
using System;

namespace ContractHome.Models.Cache
{
    public class CacheFactory
    {
        private readonly ICacheStore _cacheStore;
        private readonly ICacheKey _passwordApplyEmailResentLimitCahceKey;
        private readonly ICacheKey _trustPasswordApplyTokenCahceKey;


        public CacheFactory(ICacheStore cacheStore, 
            ICacheKey passwordApplyEmailResentLimitCahceKey,
            ICacheKey trustPasswordApplyTokenCahceKey)
        {
            _cacheStore = cacheStore;
            _passwordApplyEmailResentLimitCahceKey = passwordApplyEmailResentLimitCahceKey;
            _trustPasswordApplyTokenCahceKey = trustPasswordApplyTokenCahceKey;
        }

        public ICacheKey GetPasswordApplyEmailResentLimit(string email)
        {
            _passwordApplyEmailResentLimitCahceKey.CreateCacheKey(email);
            return _cacheStore.Get(_passwordApplyEmailResentLimitCahceKey);
        }

        public void SetPasswordApplyEmailResentLimit(string email)
        {
            _passwordApplyEmailResentLimitCahceKey.CreateCacheKey(email);
            _cacheStore.Add(_passwordApplyEmailResentLimitCahceKey);
        }

        public ICacheKey GetTrustPasswordApplyToken(string token)
        {
            _trustPasswordApplyTokenCahceKey.CreateCacheKey(token);
            return _cacheStore.Get(_trustPasswordApplyTokenCahceKey);
        }

        public void SetTrustPasswordApplyToken(string token)
        {
            _trustPasswordApplyTokenCahceKey.CreateCacheKey(token);
            _cacheStore.Add(_passwordApplyEmailResentLimitCahceKey);
        }

    }
}
