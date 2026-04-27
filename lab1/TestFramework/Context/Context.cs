using System.Collections.Generic;

namespace TestFramework.Context
{
    public class GlobalContext
    {
        private static readonly Dictionary<string, object> _sharedData = new Dictionary<string, object>();

        public void SetData(string key, object value)
        {
            if (_sharedData.ContainsKey(key))
                _sharedData[key] = value;
            else
                _sharedData.Add(key, value);
        }

        public object GetData(string key)
        {
            return _sharedData.ContainsKey(key) ? _sharedData[key] : null;
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