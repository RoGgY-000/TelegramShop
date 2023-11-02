
namespace TelegramShop.Caching
{
    using TelegramShop.DataBase;

    internal class Cache
    {
        private static Dictionary<long, object> EditCache = new ();
        private static Dictionary<long, string[]> Permissions = new (100);

        private static List<Dictionary<long, object>> Dictionaries = new ();

        public static void Init ()
        {
            AttachRange (EditCache, Permissions);
        }
        public static void AddPair (long Id, object obj) => EditCache.TryAdd (Id, obj);
        public static bool ContainsKey (long id) => EditCache.ContainsKey (id);
        public static bool TryGetValue (long Id, out object? value)
        {
            bool flag = EditCache.TryGetValue (Id, out object? obj);
            value = obj;
            return flag;
        }
        public static void SetValue (long id, object value)
        {
            if ( EditCache.ContainsKey (id) )
                EditCache[id] = value;
        }
        private static void AttachRange (params object[] dictionaries)
        {
            foreach ( object obj in dictionaries)
            {
                try
                {
                    Dictionaries.Add ((Dictionary<long, object>) obj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine (ex.ToString ());
                }
            }
        }
        public static void ClearAll ()
        {
            foreach ( Dictionary<long, object> cache in Dictionaries )
            {
                cache.Clear ();
            }
        }
        public static void RemoveUser (long userId)
        {
            foreach ( Dictionary<long, object> cache in Dictionaries )
            {
                cache.Remove (userId);
            }
        }
        public static async void LoadPermissions (long userId) => Permissions.Add (userId, await Db.GetUserPermissions (userId));
        public static string[] GetPermissions (long userId)
        {
            Permissions.TryGetValue (userId, out string[] permissions);
            return permissions;
        }
        public static async Task<bool> HasPermission (long userId, string query)
        {
            if ( Permissions.ContainsKey (userId)
                && Permissions.TryGetValue (userId, out string[]? permissions)
                && permissions is not null )
                return permissions.Contains (query);
            else
            {
                Permissions.Add (userId, await Db.GetUserPermissions (userId));
                if ( Permissions.ContainsKey (userId)
                && Permissions.TryGetValue (userId, out string[]? permissions1)
                && permissions1 is not null )
                    return permissions1.Contains (query);
            }
            return false;
        }
    }
}
