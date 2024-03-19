namespace TelegramShop.DataBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class Cache
    {
        private Dictionary<long, object> dict = new ();

        public void AddPair (long key, object value) => dict.TryAdd (key, value);
        public bool ContainsKey (long key) => dict.ContainsKey (key);
        public bool TryGetValue (long key, out object? value)
        {
            dict.TryGetValue (key, out object? val);
            value = val;
            return value is not null;
        }
        public void Clear () => dict.Clear ();
        public void RemovePair (long key) => dict.Remove (key);
        public object[] GetValues () => dict.Values.ToArray ();
        public void UpdateValue (long key, object value)
        {
            if ( dict.ContainsKey (key)
                && dict.TryGetValue (key, out object? obj)
                && obj is not null )
                dict[key] = value;
        }
    }
}
