using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orchard.Caching.Services;

namespace Piedone.Combinator.Tests.Stubs
{
    internal class StubCacheService : ICacheService
    {
        private readonly Dictionary<string, object> _entries = new Dictionary<string, object>();


        public object Get(string key)
        {
            return _entries.ContainsKey(key) ? _entries[key] : null;
        }

        public void Put(string key, object value)
        {
            _entries[key] = value;
        }

        public void Put(string key, object value, TimeSpan validFor)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            if (_entries.ContainsKey(key)) _entries.Remove(key);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        public object GetObject<T>(string key)
        {
            return _entries.ContainsKey(key) ? _entries[key] : null;
        }

        public void Put<T>(string key, T value)
        {
            _entries[key] = value;
        }

        public void Put<T>(string key, T value, TimeSpan validFor)
        {
            throw new NotImplementedException();
        }
    }
}
