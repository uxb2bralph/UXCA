using ContractHome.Models.Email.Template;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using MimeKit.Text;
using System;

namespace ContractHome.Models.Cache
{
    public class CacheFactory
    {
        private readonly ICacheStore _cacheStore;
        private readonly IEnumerable<ICacheKey> _cacheKeys;

        public CacheFactory(ICacheStore cacheStore,
            IEnumerable<ICacheKey> cacheKeys)
        {
            _cacheStore = cacheStore;
            _cacheKeys = cacheKeys;
        }

        public ICacheKey GetEmailSentCache(string email)
        {
            var aaa = _cacheKeys.OfType<EmailSentCache>().FirstOrDefault();
            aaa.CreateCacheKey(email);
            return _cacheStore.Get(aaa);
        }

        public void SetEmailSentCache(string email)
        {
            var aaa = _cacheKeys.OfType<EmailSentCache>().FirstOrDefault();
            aaa.CreateCacheKey(email);
            _cacheStore.Add(aaa);
        }

        public ICacheKey GetTokenCache(string token)
        {
            var ttt = _cacheKeys.OfType<TokenCache>().FirstOrDefault();
            ttt.CreateCacheKey(token);
            return _cacheStore.Get(ttt);
        }

        public void SetTokenCache(string token)
        {
            var ttt = _cacheKeys.OfType<TokenCache>().FirstOrDefault();
            ttt.CreateCacheKey(token);
            _cacheStore.Add(ttt);
        }

    }
}
