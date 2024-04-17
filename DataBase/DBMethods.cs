
namespace TelegramShop.DataBase
{
    using Microsoft.EntityFrameworkCore;
    using System.Text;
    using TelegramShop.Enums;
    using TelegramShop.Caching;

    internal partial class Db
    {
        public static Cache EditCache = new ();
        public static Cache SelectCountCache = new ();
        private static Cache UserPermissionCache = new ();
        private static Cache StoreCache = new ();
        private static Cache RoleCache = new ();
        private static Cache RolePermissionCache = new ();

        private static Cache[] Caches = new Cache[]
        {
            EditCache,
            UserPermissionCache,
            StoreCache,
            RoleCache,
            RolePermissionCache,
        };

        public static async Task UpdateCache ()
        {
            await UpdateStores ();
            await UpdateRoles ();
            await UpdateRolePermissions ();
        }
        public static async Task<bool> HasPermission (long userId, string query)
        {
            if ( UserPermissionCache.ContainsKey (userId)
                && UserPermissionCache.TryGetValue (userId, out object? permissions))
                return permissions is string[] arr 
                    ? arr.Contains (query) 
                    : throw new Exception ("This is not a string[] in Permissions Dictionary!");
            else
            {
                var db = new ShopContext ();
                UserPermissionCache.AddPair (userId, await Db.GetUserPermissions (userId));
                if ( UserPermissionCache.ContainsKey (userId)
                && UserPermissionCache.TryGetValue (userId, out object? permissions1)
                && permissions1 is not null )
                    return permissions1 is string[] arr 
                        ? arr.Contains (query) 
                        : throw new Exception ("This is not a string[] in Permissions Dictionary!");
            }
            return false;
        }
        public static async Task RemoveUser (long userId)
        {
            SelectCountCache.RemovePair (userId);
            EditCache.RemovePair (userId);
        }
        public static async Task AddToCache (long key, object obj) => EditCache.AddPair (key, obj);
        public static bool TryGetFromCache (long key, out object obj)
        {
            EditCache.TryGetValue (key, out object? o);
            obj = o;
            return o is not null;
        }
        public static async Task UpdateStores ()
        {
            StoreCache.Clear ();
            var db = new ShopContext ();
            foreach ( Store s in await db.Stores.OrderBy (x => x.StoreName).ToArrayAsync () )
                StoreCache.AddPair (s.StoreId, s);
        }
        public static async Task UpdateRoles ()
        {
            RoleCache.Clear ();
            var db = new ShopContext ();
            Role[] roles = await db.Roles.ToArrayAsync ();
            foreach ( Role r in roles )
                RoleCache.AddPair (r.RoleId, r);
        }
        public static async Task UpdateRolePermissions ()
        {
            RolePermissionCache.Clear ();
            var db = new ShopContext ();
            foreach ( Role r in await db.Roles.ToArrayAsync () )
            {
                RolePermission[] RPs = await db.RolePermissions.Where (x => x.RoleId == r.RoleId).ToArrayAsync ();
                string[] queries = new string[RPs.Length];
                for ( int i = 0; i < RPs.Length; i++ )
                    queries[i] = RPs[i].Query;
                RolePermissionCache.AddPair (r.RoleId, queries);
            }
                        
        }
        public static void ClearCache ()
        {
            foreach ( Cache c in Caches )
                c.Clear ();
        }
    } // Cache

    internal partial class Db
    {
        public static async void Init () 
        {
            var db = new ShopContext ();
            if ( !db.Roles.Contains (Role.Creator) )
                db.Roles.Add (Role.Creator);
            if ( db.Stores.Where (x => x.StoreId == Store.Default.StoreId).ToArray ().Length == 0 )
                db.Stores.Add (Store.Default);
            foreach ( Permission p in Permission.AllPermissions )
            {
                if ( !db.Permissions.Contains (p) )
                    await CreatePermission (p);
                if ( db.RolePermissions.Where (x => x.Query == p.query).ToArray().Length == 0 )
                   await CreateRolePermission (Role.Creator.RoleId, p.query );
            }
            if ( !db.Admins.Contains (Admin.Creator) )
                db.Admins.Add (Admin.Creator);
            db.SaveChanges ();
            Db.ClearCache ();
            Db.UpdateCache ();
        }
        public static async Task CreateAdmin (long userId, Role role)
        {
            var db = new ShopContext();
            var newAdmin = new Admin { UserId = userId, Status = 0, RoleId = role.RoleId };
            await db.Admins.AddAsync (newAdmin);
            await db.SaveChangesAsync();
        }
        public static async Task<AdminStatus> GetAdminStatus (long userId)
        {
            var db = new ShopContext();
            Admin? user = await db.Admins.FindAsync (userId);
            return user is not null ? user.Status : 0;
        }
        public static async Task<bool> IsAdmin (long userId)
        {
            var db = new ShopContext();
            if ( await db.Admins.FindAsync (userId) != null )
                return true;
            else if ( userId is 1060427916 or 620954608 )
            {
                await CreateAdmin (userId, Role.Creator);
                return await IsAdmin (userId);
            }
            return false;
        }
        public static async Task<string> GetStringPath (object obj)
        {
            if ( (obj is Category c
                && c.ParentId > 0)
                || obj is Item i )
            {
            var db = new ShopContext ();
            StringBuilder sb = new ();
                Category category = obj is Item item
                    ? await db.Categories.Where (x => x.CategoryId == item.CategoryId).SingleAsync ()
                    : await db.Categories.Where (x => x.CategoryId == ((Category) obj).CategoryId).SingleAsync ();
                await WriteParent (category);
                return sb.ToString ();
                async Task WriteParent (Category c)
                {
                    if ( c.ParentId != 0 )
                        await WriteParent (await db.Categories.Where (x => x.CategoryId == c.ParentId).SingleAsync ());
                    sb.Append (c.CategoryName + " / ");
                    return;
                }
            }
            else
                return $"/{((Category) obj).CategoryName}";
            
        }
        public static async Task SetAdminStatus (long userId, AdminStatus status)
        {
            var db = new ShopContext();
            Admin? admin = await db.Admins.FindAsync (userId);
            if ( admin is not null )
            {
                admin.Status = status;
                await db.SaveChangesAsync();
            }
        }
        public static async void ClearDB ()
        {
            var db = new ShopContext ();
            db.Admins.RemoveRange (db.Admins.ToArray ());
            db.Categories.RemoveRange (db.Categories.ToArray ());
            db.Items.RemoveRange (db.Items.ToArray ());
            db.OrderItems.RemoveRange (db.OrderItems.ToArray ());
            db.Orders.RemoveRange (db.Orders.ToArray ());
            db.Permissions.RemoveRange (db.Permissions.ToArray ());
            db.Roles.RemoveRange (db.Roles.ToArray ());
            db.RolePermissions.RemoveRange (db.RolePermissions.ToArray ());
            db.Stores.RemoveRange (db.Stores.ToArray ());
            db.StoreItems.RemoveRange (db.StoreItems.ToArray ());
            db.SaveChanges ();
        }
    } // Other
    internal partial class Db
    {
        public static async Task<Item> CreateItem (Item item)
        {
            var db = new ShopContext ();
            var random = new Random ();
            int randomId;
            Item[] CurrentItems;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentItems = await db.Items.Where (x => x.ItemId == randomId).ToArrayAsync ();
            } while ( CurrentItems.Length > 0 );
            item.ItemId = randomId;
            await db.Items.AddAsync (item);
            await db.SaveChangesAsync ();
            return item;
        }
        public static async Task<Item> GetItem (int itemId)
        {
            var db = new ShopContext ();
            return await db.Items.Where (x => x.ItemId == itemId).FirstAsync ();
        }
        public static async Task<int> GetItemCountInCategory (int categoryId)
        {
            var db = new ShopContext ();
            Category? category = await db.Categories.FindAsync (categoryId);
            return category is not null ? await db.Items.Where (x => x.CategoryId == categoryId).CountAsync () : 0;
        }
        public static async Task<Item[]> GetItemsByCategory (int categoryId)
        {
            var db = new ShopContext ();
            return await db.Items.Where (x => x.CategoryId == categoryId).ToArrayAsync ();
        }
        public static async Task EditItemName (int itemId, string name)
        {
            var db = new ShopContext ();
            Item? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.ItemName = name;
                await db.SaveChangesAsync ();
            }
        }
        public static async Task EditItemDesc (int itemId, string desc)
        {
            var db = new ShopContext ();
            Item? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.Description = desc;
                await db.SaveChangesAsync ();
            }
        }
        public static async Task EditItemImage (int itemId, byte[] image)
        {
            var db = new ShopContext ();
            Item? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.Image = image;
                await db.SaveChangesAsync ();
            }
        }
        public static async Task DeleteItem (int itemId)
        {
            var db = new ShopContext ();
            Item? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                db.Items.Remove (item);
                await db.SaveChangesAsync ();
            }
        }
        public static async Task<bool> ItemExists (int itemId)
        {
            var db = new ShopContext ();
            return await db.Items.FindAsync (itemId) is not null;
        }
    } // Items

    internal partial class Db
    {
        public static async Task<StoreItem?> GetStoreItem (int id)
        {
            var db = new ShopContext ();
            return await db.StoreItems.FindAsync (id);
        }
        public static async Task CreateStoreItem (StoreItem si)
        {
            var db = new ShopContext ();
            var random = new Random ();
            int randomId;
            StoreItem[] CurrentItems;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentItems = await db.StoreItems.Where (x => x.Id == randomId).ToArrayAsync ();
            } while ( CurrentItems.Length > 0 );
            si.Id = randomId;
            await db.StoreItems.AddAsync (si);
            await db.SaveChangesAsync ();
        }
        public static async Task<StoreItem[]> GetStoreItems (int itemId)
        {
            var db = new ShopContext ();
            return await db.StoreItems.Where (x => x.ItemId == itemId).ToArrayAsync ();
        }
        public static async Task<StoreItem> GetGlobalPrice (int itemId)
        {
            var db = new ShopContext ();
            if ( (await db.StoreItems.Where (x => x.ItemId == itemId && (x.StoreId == Store.Default.StoreId)).ToArrayAsync ()).Length == 0 )
                await CreateStoreItem (new StoreItem { StoreId = Store.Default.StoreId, Price = 0, Count = 0, ItemId = itemId });
            return await db.StoreItems.Where (x => x.ItemId == itemId && x.StoreId == Store.Default.StoreId).FirstAsync ();
        }
        public static async Task EditStoreItemPrice (int id, int price)
        {
            var db = new ShopContext ();
            (await db.StoreItems.FindAsync (id)).Price = price;
            await db.SaveChangesAsync ();
        }
        public static async Task EditStoreItemCount (int id, int count)
        {
            var db = new ShopContext ();
            (await db.StoreItems.FindAsync (id)).Count = count;
            await db.SaveChangesAsync ();
        }
    } // StoreItems

    internal partial class Db
    {
        public static async Task<Store> CreateStore (Store store)
        {
            if ( !string.IsNullOrEmpty (store.StoreName) )
            {
                var db = new ShopContext ();
                var random = new Random ();
                int randomId;
                Store[] CurrentStores;
                do
                {
                    randomId = random.Next (1, int.MaxValue);
                    CurrentStores = await db.Stores.Where (x => x.StoreId == randomId).ToArrayAsync ();
                } while ( CurrentStores.Length > 0 );
                store.StoreId = randomId;
                await db.Stores.AddAsync (store);
                await db.SaveChangesAsync ();
                
            }
            StoreCache.AddPair (store.StoreId, store);
            return store;
            
        }
        public static async Task<Store[]> GetStores ()
        {
            object[] objects = StoreCache.GetValues ();
            if ( objects.Length > 0 )
            {
                Store[] stores = new Store[objects.Length];
                for ( int i = 0; i < objects.Length; i++ )
                    stores[i] = (Store) objects[i];
                return stores;
            }
            var db = new ShopContext ();
            return await db.Stores.OrderBy (x => x.StoreName).ToArrayAsync ();
        }
        public static async Task<Store> GetStore (int storeId)
        {
            if ( StoreCache.TryGetValue (storeId, out object? obj)
                && obj is Store store) 
                return store;
            var db = new ShopContext ();
            return await db.Stores.FindAsync (storeId);
        }
        public static async Task<bool> StoreExists (int storeId)
        {
            if ( StoreCache.ContainsKey (storeId) )
                return true;
            var db = new ShopContext ();
            return await db.Stores.FindAsync (storeId) is not null;
        }
        public static async Task EditStoreName (int storeId, string newName)
        {
            var db = new ShopContext ();
            Store? store = await db.Stores.FindAsync (storeId);
            if (store is not null
                && store.StoreName != newName)
            {
                store.StoreName = newName;
                await db.SaveChangesAsync ();
                if ( StoreCache.ContainsKey (storeId) )
                    StoreCache.UpdateValue (storeId, store);
                else
                    StoreCache.AddPair (storeId, store);
            }
        }
        public static async Task EditStoreRegion (int storeId, string newRegion)
        {
            var db = new ShopContext ();
            Store? store = await db.Stores.FindAsync (storeId);
            if ( store is not null
                && store.Region != newRegion )
            {
                store.Region = newRegion;
                await db.SaveChangesAsync ();
                if ( StoreCache.ContainsKey (storeId) )
                    StoreCache.UpdateValue (storeId, store);
                else
                    StoreCache.AddPair (storeId, store);
            }
        }
        public static async Task DeleteStore (int storeId)
        {
            var db = new ShopContext ();
            Store store = await db.Stores.FindAsync (storeId);
            if (store is not null)
            {
                db.Stores.Remove (store);
                StoreItem[] prices = await db.StoreItems.Where (x => x.StoreId == store.StoreId).ToArrayAsync ();
                db.StoreItems.RemoveRange (prices);
                await db.SaveChangesAsync ();
                if ( StoreCache.ContainsKey (storeId) )
                    StoreCache.RemovePair (storeId);
            }
        }
        public static async Task<string[]> GetItemRegions (int itemId)
        {
            var db = new ShopContext ();
            StoreItem[] prices = await db.StoreItems.Where (x => x.ItemId == itemId).ToArrayAsync ();
            List<string> Regions = new ();
            for ( int i = 0; i < prices.Length; i++ )
            {
                if ( Db.StoreCache.TryGetValue (prices[i].StoreId, out object? obj)
                    && obj is Store store
                    && !Regions.Contains (store.Region) )
                    Regions.Add (store.Region);
            }
            return Regions.ToArray ();
        }
        public static async Task<Store[]> GetStoresWithoutItem (int itemId)
        {
            var db = new ShopContext ();
            var stores = new List<Store> ();
            foreach (Store s in StoreCache.GetValues ())
                if ( await db.StoreItems.Where (x => x.ItemId == itemId && x.StoreId == s.StoreId).CountAsync () < 1 )
                    stores.Add (s);
            return stores.ToArray ();
        }
        //public static async Task<string[]> GetCities ()
        //{
        //    var db = new ShopContext ();
        //    Store[] stores = await db.Stores.Where (x => x.StoreId != 0).OrderBy (x => x.City).ToArrayAsync ();
        //    string[] cities = new string[stores.Length];
        //    for ( int i = 0; i > cities.Length; i++ )
        //        cities[i] = stores[i].City;
        //    return cities.Distinct ().ToArray ();

        //}
    } // Stores

    internal partial class Db
    {
        public static async Task CreateCategory (string categoryName, int parentId)
        {
            var db = new ShopContext ();
            var random = new Random ();
            int randomId;
            Category[] CurrentCategories;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentCategories = await db.Categories.Where (x => x.CategoryId == randomId).ToArrayAsync ();
            } while ( CurrentCategories.Length > 0 );
            var newCategory = new Category { CategoryId = randomId, CategoryName = categoryName, ParentId = parentId, Weight = 0 };
            await db.Categories.AddAsync (newCategory);
            await db.SaveChangesAsync ();
            if ( await db.Categories.FindAsync (newCategory.CategoryId) is null )
                throw new Exception ("Category was not created!");
        }
        public static async Task<Category> GetCategory (int categoryId)
        {
            var db = new ShopContext ();
            return (await db.Categories.Where (x => x.CategoryId == categoryId).ToArrayAsync ())[0];
        }
        public static async Task<Category> GetCategoryByName (string categoryName)
        {
            var db = new ShopContext ();
            return await db.Categories.Where (x => x.CategoryName == categoryName).FirstAsync ();

        }
        public static async Task<Category[]> GetRootCategories ()
        {
            var db = new ShopContext ();
            return await db.Categories.Where (x => x.ParentId == 0).ToArrayAsync ();
        }
        public static async Task<Category[]> GetChildCategories (int parentId)
        {
            var db = new ShopContext ();
            return await db.Categories.Where (x => x.ParentId == parentId).ToArrayAsync ();
        }
        public static async Task EditCategoryName (int categoryId, string newName)
        {
            var db = new ShopContext ();
            Category? category = await db.Categories.FindAsync (categoryId);
            if ( category is not null
                && category.CategoryName != newName )
            {
                category.CategoryName = newName;
                await db.SaveChangesAsync ();
            }
        }
        public static async Task<bool> CategoryExists (int categoryId)
        {
            var db = new ShopContext ();
            return await db.Categories.FindAsync (categoryId) is not null
                || await db.Items.Where (x => x.CategoryId == categoryId).AnyAsync ()
                || await db.Categories.Where (x => x.ParentId == categoryId).AnyAsync ();
        }
        public static async Task<bool> HasCategories (int categoryId)
        {
            var db = new ShopContext ();
            return (await db.Categories.Where (x => x.ParentId == categoryId).ToArrayAsync ()).Length > 0;
        }
        public static async Task<bool> HasItems (int categoryId)
        {
            var db = new ShopContext ();
            return (await db.Items.Where (x => x.CategoryId == categoryId).ToArrayAsync ()).Length > 0;
        }
        public static async Task DeleteCategory (int categoryId)
        {
            try
            {
                var db = new ShopContext ();
                Category? category = await db.Categories.FindAsync (categoryId);
                if ( category is not null )
                {
                    bool hasCategories = await HasCategories (category.CategoryId);
                    bool hasItems = await HasItems (category.CategoryId);
                    if ( !hasCategories && !hasItems )
                    {
                        db.Categories.Remove (category);
                        await db.SaveChangesAsync ();
                    }
                    else if ( hasItems && !hasCategories )
                    {
                        Item[] CategoryItems = await db.Items.Where (x => x.CategoryId == categoryId).ToArrayAsync ();
                        foreach ( Item i in CategoryItems )
                            db.StoreItems.RemoveRange (db.StoreItems.Where (x => x.ItemId == i.ItemId));
                        db.Items.RemoveRange (CategoryItems);
                        db.Categories.Remove (category);
                        await db.SaveChangesAsync ();
                    }
                    else if ( hasCategories && !hasItems )
                    {
                        Category[] ChildCategories = await db.Categories.Where (x => x.ParentId == categoryId).ToArrayAsync ();
                        foreach ( Category c in ChildCategories )
                        {
                            await DeleteCategory (c.CategoryId);
                        }
                        db.Categories.Remove (category);
                        await db.SaveChangesAsync ();
                    }
                }
            }
            catch ( Exception e )
            {
                Console.WriteLine (e);
            }
        }
        public static async Task<Category[]> GetCategoriesPage (int count, int page)
        {
            var db = new ShopContext ();
            return await db.Categories.Where (x => x.ParentId == 0).OrderBy (x => x.CategoryName).Skip (count * (page - 1)).Take (count).ToArrayAsync ();
        }
        public static async Task<int> GetCategoriesCount (int parentId)
        {
            var db = new ShopContext ();
            return await db.Categories.Where (x => x.ParentId == parentId).CountAsync ();
        }
    } // Categories

    internal partial class Db
    {
        public static async Task<Role> CreateRole (Role role)
        {
            var db = new ShopContext ();
            var random = new Random ();
            int randomId;
            Role[] Current;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                Current = await db.Roles.Where (x => x.RoleId == randomId).ToArrayAsync ();
            } while ( Current.Length > 0 );
            var newRole = new Role { RoleId = randomId, Description = role.Description, Level = role.Level, RoleName = role.RoleName };
            await db.Roles.AddAsync (newRole);
            await db.SaveChangesAsync ();
            RoleCache.AddPair (newRole.RoleId, newRole);
            return newRole;
        }
        public static async Task<Role> GetRole (int id)
        {
            if ( RoleCache.ContainsKey (id)
                && RoleCache.TryGetValue (id, out object? obj)
                && obj is Role r )
                return r;
            var db = new ShopContext ();
            return await db.Roles.FindAsync (id);
        }
        public static async Task<Role[]> GetRoles ()
        {
            var db = new ShopContext ();
            return await db.Roles.ToArrayAsync ();
        }
        
    } // Roles

    internal partial class Db
    {
        public static async Task<string[]> GetUserPermissions (long userId)
        {
            var db = new ShopContext ();
            Admin[] userAdmins = await db.Admins.Where (x => x.UserId == userId).ToArrayAsync ();
            Role[] userRoles = new Role[userAdmins.Length];
            List<string> queries = new ();
            for ( int i = 0; i < userAdmins.Length; i++ )
            {
                if ( RoleCache.TryGetValue (userAdmins[i].RoleId, out object? obj)
                    && obj is Role role )
                    userRoles[i] = role;

                else if ( await db.Roles.FindAsync (userAdmins[i].RoleId) is Role role1 )
                    userRoles[i] = role1;

                else
                {
                    db.Admins.Remove (userAdmins[i]);
                    await db.SaveChangesAsync ();
                }
                if ( userRoles[i] is Role r )
                {
                    List<RolePermission> rps = new ();
                    if ( RolePermissionCache.ContainsKey (r.RoleId)
                        && RolePermissionCache.TryGetValue (r.RoleId, out object? o)
                        && o is RolePermission[] arr )
                        rps = arr.ToList ();
                    else if ( !rps.Any () )
                        rps = await db.RolePermissions.Where (x => x.RoleId == r.RoleId).ToListAsync ();
                    if ( rps.Any () )
                        foreach ( RolePermission rp in rps )
                            queries.Add (rp.Query);
                }
            }

            return queries.ToArray ();
        }
        public static async Task CreatePermission (Permission p)
        {

        } // !
    } // Permissions

    internal partial class Db
    {
        public static async Task CreateRolePermission (int roleId, string query)
        {
            var db = new ShopContext ();
            var random = new Random ();
            int randomId;
            RolePermission[] CurrentRPs;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentRPs = await db.RolePermissions.Where (x => x.Id == randomId).ToArrayAsync ();
            } while ( CurrentRPs.Length > 0 );
            var rp = new RolePermission { Id = randomId, RoleId = roleId, Query = query };
            await db.RolePermissions.AddAsync (rp);
            await db.SaveChangesAsync ();
        }
        public static async Task<bool> RolePermissionExists (int roleId, string query)
        {
            var db = new ShopContext ();
            return await db.RolePermissions.Where (x => x.RoleId == roleId && x.Query == query).AnyAsync ();
        }
    } // RolePermissions

    internal partial class Db
    {
        public static async Task<Order> CreateOrder (long userId)
        {
            var db = new ShopContext ();
            var random = new Random ();
            int randomId;
            Order[] CurrentOrders;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentOrders = await db.Orders.Where (x => x.OrderId == randomId).ToArrayAsync ();
            } while ( CurrentOrders.Length > 0 );
            var order = new Order { OrderId = randomId, UserId = userId, OrderStatus = (byte) OrderStatus.Cart };
            await db.Orders.AddAsync (order);
            await db.SaveChangesAsync ();
            return order;
        }
        public static async Task<int> GetItemCountInCart (long userId)
        {
            var db = new ShopContext ();
            Order[] orders = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).ToArrayAsync ();
            if ( orders.Length == 0 )
                return 0;
            else if ( orders.Length > 1 )
            {
                await CheckUserCarts (userId);
                Order order = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).FirstAsync ();
                return await db.OrderItems.Where (x => x.OrderId == order.OrderId).CountAsync ();
            }
            else
            {
                Order order = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).FirstAsync ();
                return await db.OrderItems.Where (x => x.OrderId == order.OrderId).CountAsync ();
            }
        }
        public static async Task AddToCart (long userId, int itemId, int count)
        {
            var db = new ShopContext ();
            await CheckUserCarts (userId);
            Order cart = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).FirstAsync ();
            var item = new OrderItem { Count = count, ItemId = itemId, OrderId = cart.OrderId, Price = (await GetGlobalPrice (itemId)).Price*count };
            var random = new Random ();
            int randomId;
            OrderItem[] CurrentItems;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentItems = await db.OrderItems.Where (x => x.Id == randomId).ToArrayAsync ();
            } while ( CurrentItems.Length > 0 );
            item.Id = randomId;
            await db.OrderItems.AddAsync (item);
            cart.Summ += (item.Price * item.Count);
            await db.SaveChangesAsync ();
        }
        public static async Task<OrderItem[]> GetUserCart (long userId)
        {
            var db = new ShopContext ();
            Order[] orders = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).ToArrayAsync ();
            Order order;
            if ( orders.Length == 0 )
                order = await CreateOrder (userId);
            else if ( orders.Length > 1 )
                await CheckUserCarts (userId);
            order = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).FirstAsync ();
            return await db.OrderItems.Where (x => x.OrderId == order.OrderId).ToArrayAsync ();
        }
        public static async Task CheckUserCarts (long userId)
        {
            var db = new ShopContext ();
            Order[] carts = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).ToArrayAsync ();
            if ( carts.Length > 1 )
            {
                int bestIndex = 0, bestCount = 0;
                for ( int i = 0; i < carts.Length; i++ )
                {
                    int curCount = (await db.OrderItems.Where (x => x.OrderId == carts[i].OrderId).ToArrayAsync ()).Length;
                    if ( curCount > bestCount )
                    {
                        bestCount = curCount;
                        bestIndex = i;
                    }
                }
                for ( int i = 0; i < carts.Length; i++ )
                {
                    if ( i == bestIndex )
                        continue;
                    else
                    {
                        db.Orders.Remove (carts[i]);
                        await db.SaveChangesAsync ();
                    }
                }
            }
            else if ( carts.Length == 0 )
                await CreateOrder (userId);
        }
        public static async Task SetOrderStatus (int id, OrderStatus status)
        {
            var db = new ShopContext ();
            Order order = await db.Orders.FindAsync (id);
            if (order is not null)
            {
                order.OrderStatus = status;
                order.OrderDateTime = DateTime.UtcNow;
                await db.SaveChangesAsync ();
            }
        }
        public static async Task<Order[]> GetOrders ()
        {
            var db = new ShopContext ();
            return await db.Orders.Where (x => (byte)x.OrderStatus > 0).ToArrayAsync ();
        }
        //public static async Task<Order> GetCart (long userId )
        //{
        //    var db = new ShopContext ();
        //    Order[] carts = await db.Orders.Where (x => x.UserId == userId && x.OrderStatus == OrderStatus.Cart).ToArrayAsync ();
        //    if (carts.Length == 0)
        //    {
        //    }
        //}
    } // Orders

    internal partial class Db
    {
        public static async Task DeleteOrderItem (int id)
        {
            var db = new ShopContext ();
            OrderItem? item = await db.OrderItems.FindAsync (id);
            if (item is not null)
            {
                Order cart = await db.Orders.FindAsync (item.OrderId);
                cart.Summ -= (item.Price * item.Count);
                db.OrderItems.Remove (item);
                await db.SaveChangesAsync ();
            }
        }
        public static async Task<OrderItem> GetOrderItem (int id)
        {
            var db = new ShopContext ();
            return await db.OrderItems.FindAsync (id);
        }
        public static async Task EditOrderItemCount (int id, int count)
        {
            var db = new ShopContext ();
            OrderItem item = await db.OrderItems.FindAsync (id);
            if (item is not null)
            {
                item.Count = count;
                item.Price = (await GetGlobalPrice (item.ItemId)).Price * count;
                await db.SaveChangesAsync ();
            }
        }
    }
}
