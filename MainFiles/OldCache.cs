
namespace TelegramShop.Caching
{
    using TelegramShop.DataBase;

    internal class OldCache
    {
        private static Dictionary<long, object> EditCache = new ();
        private static Dictionary<long, object> Permissions = new ();
        private static Dictionary<int, Store> Stores = new ();

        private static List<Dictionary<long, object>> Dictionaries = new ();

        public static async void LoadPermissions (long userId) => Permissions.Add (userId, await Db.GetUserPermissions (userId));
        public static string[] GetPermissions (long userId)
        {
            Permissions.TryGetValue (userId, out object permissions);
            return permissions is string[] arr ? arr : throw new Exception ("This is not a string[] in Permissions Dictionary!");
        } // !
        public static async Task AddStore (Store store) => Stores.TryAdd (store.StoreId, store);
        public static async Task<Store> GetStore (int id)
        {
            if ( Stores.ContainsKey (id) )
            {
                Stores.TryGetValue (id, out Store store);
                return store;
            }
            else if ( await Db.StoreExists (id) )
                return await Db.GetStore (id);
            else throw new Exception ("Store not found!");
        }
        public static async Task EditStoreName (int id, string name)
        {
            if ( Stores.ContainsKey (id))
            {
                Stores[id].StoreName = name;
                await Db.EditStoreName (id, name);
            }
            else if (await Db.StoreExists (id))
            {
                await Db.EditStoreName (id, name);
                Stores.TryAdd (id, await Db.GetStore (id));
            }
        }
    }
}
