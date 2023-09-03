
namespace TelegramShop.DataBase
{
    using Microsoft.EntityFrameworkCore;
    using System.Text;
    internal partial class Db
    {
        public static void init ()
        {
            var db = new ShopContext ();
            if ( !db.Roles.Any () )
                db.Roles.Add (Role.Creator);
            if ( !db.Stores.Any () )
                db.Stores.Add (Store.Default);
            db.SaveChanges ();
        }
        public static async Task CreateOrder (long userId)
        {
            var db = new ShopContext();
            var random = new Random();
            int randomId;
            Order[] CurrentOrders;
            do
            {
                randomId = random.Next (1, int.MaxValue);
                CurrentOrders = await db.Orders.Where (x => x.OrderId == randomId).ToArrayAsync();
            } while ( CurrentOrders.Length > 0 );
            var order = new Order { OrderId = randomId, UserId = userId, OrderStatus = (byte) OrderStatus.Cart};
            await db.Orders.AddAsync (order);
            await db.SaveChangesAsync ();
        }
        public static async Task CreateAdmin (long userId, Role role)
        {
            var db = new ShopContext();
            var newAdmin = new Admin { UserId = userId, Status = 0, Role = role };
            await db.Admins.AddAsync (newAdmin);
            await db.SaveChangesAsync();
        }
        public static async Task<OrderItem[]> GetUserCart (long userId)
        {
            var db = new ShopContext();
            Order[] orders = await db.Orders.Where (x => x.UserId == userId).ToArrayAsync();
            if ( orders.Length == 0 )
            {
                await CreateOrder (userId);
                orders = await db.Orders.Where (x => x.UserId == userId).ToArrayAsync();
            }
            Order order = orders.Where (x => x.OrderStatus == (byte) OrderStatus.Cart).First();
            return await db.OrderItems.Where (x => x.OrderId == order.OrderId).ToArrayAsync();
        }
        public static async Task<AdminStatus> GetAdminStatus (long userId)
        {
            var db = new ShopContext();
            Admin? user = await db.Admins.FindAsync (userId);
            return user is not null ? user.Status : 0;
        }
        public static async Task AddToCart (long userId, int itemId, byte count)
        {
            var db = new ShopContext();
            OrderItem[] Cart = await GetUserCart (userId);
            if (Cart == null)
                await CreateOrder (userId);
            Order[] orders = await db.Orders.Where (x => x.UserId == userId).ToArrayAsync();
            Order order = orders.Where (x => x.OrderStatus == (byte) OrderStatus.Cart).ToArray()[0];
            var ItemToAdd = new OrderItem { OrderId = order.OrderId, Count = count, ItemId = itemId };
            await db.OrderItems.AddAsync (ItemToAdd);
            await db.SaveChangesAsync();
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
            if ( obj is Item or Category )
            {
                var db = new ShopContext ();
                StringBuilder sb = new ();
                Category category = obj is Item item
                    ? await db.Categories.Where (x => x.CategoryId == item.CategoryId).SingleAsync ()
                    : await db.Categories.Where ( x => x.CategoryId ==((Category) obj).ParentId).SingleAsync ();
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
            return string.Empty;
            
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
    }
    internal partial class Db
    {
        public static async Task CreateItem (Item item)
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
        public static async Task CreateStoreItem (StoreItem si)
        {
            var db = new ShopContext ();
            await db.StoreItems.AddAsync (si);
            await db.SaveChangesAsync ();
        }
        public static async Task<StoreItem[]> GetStoreItems (int itemId)
        {
            var db = new ShopContext ();
            return await db.StoreItems.Where (x => x.ItemId == itemId).ToArrayAsync ();
        }
    } // StoreItems

    internal partial class Db
    {
        public static async Task CreateStore (Store store)
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
            
        }
        public static async Task<Store[]> GetStores ()
        {
            var db = new ShopContext ();
            return await db.Stores.OrderBy (x => x.StoreName).ToArrayAsync ();
        }
        public static async Task<Store> GetStore (int storeId)
        {
            var db = new ShopContext ();
            return await db.Stores.Where (x => x.StoreId == storeId).FirstAsync ();
        }
        public static async Task<bool> StoreExists (int storeId)
        {
            var db = new ShopContext ();
            return await db.Stores.FindAsync (storeId) is not null;
        }
        public static async Task<string[]> GetCities ()
        {
            var db = new ShopContext ();
            Store[] stores = await db.Stores.Where (x => x.StoreId != 0).OrderBy (x => x.City).ToArrayAsync ();
            string[] cities = new string[stores.Length];
            for ( int i = 0; i > cities.Length; i++ )
                cities[i] = stores[i].City;
            return cities.Distinct ().ToArray ();

        }
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
            var newCategory = new Category { CategoryId = randomId, CategoryName = categoryName, ParentId = parentId };
            await db.Categories.AddAsync (newCategory);
            await db.SaveChangesAsync ();
        }
        public static async Task<Category> GetCategory (int categoryId)
        {
            var db = new ShopContext ();
            return await db.Categories.Where (x => x.CategoryId == categoryId).FirstAsync ();
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
                        db.Items.RemoveRange (CategoryItems);
                        await db.SaveChangesAsync ();
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
    } // Categories

    internal partial class Db
    {
        public static async Task CreateRole (Role role)
        {
            if ( !string.IsNullOrEmpty (role.RoleName) )
            {
                var db = new ShopContext ();
                await db.Roles.AddAsync (role);
                await db.SaveChangesAsync ();
            }
        }
    } // Roles
}
