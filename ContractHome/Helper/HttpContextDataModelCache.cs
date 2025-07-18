using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ContractHome.Helper
{
    public partial class HttpContextDataModelCache
    {
        protected IMemoryCache? _cache;
        private readonly ConcurrentDictionary<string, bool> _keyMap = new();

        public HttpContextDataModelCache(HttpContext context)
        {
            _cache = context.RequestServices.GetService(typeof(IMemoryCache)) as IMemoryCache;
        }

        public Object? DataItem
        {
            get
            {
                if (_cache != null && _cache.TryGetValue(KeyName, out object item))
                {
                    return item;
                }
                return null;
            }
            set
            {
                if (_cache != null && value != null)
                {
                    double timeout = Properties.Settings.Default.SessionTimeoutInMinutes;
                    _cache.Set(KeyName, value, TimeSpan.FromMinutes(timeout));
                    _keyMap.TryAdd(KeyName, true);
                }
            }
        }

        private String KeyName { get; set; } = String.Empty;

        public void Remove(String keyName)
        {
            if (_cache == null || String.IsNullOrEmpty(keyName))
            {
                return;
            }
            _cache.Remove(keyName);
            _keyMap.TryRemove(keyName, out _);
        }

        public void Clear()
        {
            if (_cache == null)
            {
                return;
            }

            foreach (var key in _keyMap.Keys)
            {
                _cache.Remove(key);
            }

            _keyMap.Clear();
        }

        public Object? this[String index]
        {
            get
            {
                KeyName = index;
                return this.DataItem;
            }
            set
            {
                KeyName = index;
                this.DataItem = value;
            }
        }

    }
}