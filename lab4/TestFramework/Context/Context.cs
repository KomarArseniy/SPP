using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TestFramework.Context
{
    public class GlobalContext
    {
        private static readonly ConcurrentDictionary<string, object> _sharedData = new ConcurrentDictionary<string, object>();

        public void SetData(string key, object value)
        {
            _sharedData.AddOrUpdate(key, value, (k, oldValue) => value);
        }

        public object GetData(string key)
        {
            return _sharedData.TryGetValue(key, out var value) ? value : null;
        }

        public T GetData<T>(string key)
        {
            var data = GetData(key);
            return data == null ? default : (T)data;
        }

        public void Clear()
        {
            _sharedData.Clear();
        }
    }
}