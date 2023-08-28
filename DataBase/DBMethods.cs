
namespace TelegramShop.DataBase
{
    using Microsoft.EntityFrameworkCore;
    using System.Text;
    internal class DBMethods
    {
        public static async Task CreateItem (int categoryId, string name, string? desc, int price)
        {
            var db = new ShopContext();
            var random = new Random();
            int randomId;
            Items[] CurrentItems;
            do 
            {
                randomId = random.Next (int.MaxValue);
                CurrentItems = await db.Items.Where (x => x.ItemId == randomId).ToArrayAsync();
            } while ( CurrentItems.Length > 0 );
            var newItem = new Items { ItemId = randomId, CategoryId = categoryId, ItemName = name, Description = desc, Price = price };
            await db.Items.AddAsync (newItem);
            await db.SaveChangesAsync();
        }
        public static async Task CreateCategory (string categoryName, int parentId)
        {
            var db = new ShopContext();
            var random = new Random();
            int randomId;
            Categories[] CurrentCategories;
            do
            {
                randomId = random.Next (int.MaxValue);
                CurrentCategories = await db.Categories.Where (x => x.CategoryId == randomId).ToArrayAsync();
            } while ( CurrentCategories.Length > 0 );
            var newCategory = new Categories { CategoryId = randomId, CategoryName = categoryName, ParentId = parentId };
            await db.Categories.AddAsync (newCategory);
            await db.SaveChangesAsync ();
        }
        public static async Task CreateOrder (long userId)
        {
            var db = new ShopContext();
            var random = new Random();
            int randomId;
            Orders[] CurrentOrders;
            do
            {
                randomId = random.Next (int.MaxValue);
                CurrentOrders = await db.Orders.Where (x => x.OrderId == randomId).ToArrayAsync();
            } while ( CurrentOrders.Length > 0 );
            var order = new Orders { OrderId = randomId, UserId = userId, OrderStatus = (byte) OrderStatus.Cart};
            await db.Orders.AddAsync (order);
            await db.SaveChangesAsync ();
        }
        public static async Task CreateAdmin (long userId)
        {
            var db = new ShopContext();
            var newAdmin = new Admins { UserId = userId, Status = 0 };
            await db.Admins.AddAsync (newAdmin);
            await db.SaveChangesAsync();
        }
        public static async Task<Items> GetItem (int itemId)
        {
            var db = new ShopContext();
            return await db.Items.Where ( x => x.ItemId == itemId).FirstAsync();
        }
        public static async Task<int> GetItemCountInCategory (int categoryId)
        {
            var db = new ShopContext();
            Categories? category = await db.Categories.FindAsync (categoryId);
            return category is not null ? await db.Items.Where (x => x.CategoryId == categoryId).CountAsync() : 0;
        }
        public static async Task<Categories> GetCategory (int categoryId)
        {
            var db = new ShopContext();
            return await db.Categories.Where (x => x.CategoryId == categoryId).FirstAsync();
        }
        public static async Task<Categories> GetCategoryByName (string categoryName)
        {
            var db = new ShopContext();
            return await db.Categories.Where (x => x.CategoryName == categoryName).FirstAsync();

        }
        public static async Task<Categories[]> GetRootCategories()
        {
            var db = new ShopContext();
            return await db.Categories.Where (x => x.ParentId == 0).ToArrayAsync();
        }
        public static async Task<Categories[]> GetChildCategories (int parentId)
        {
            var db = new ShopContext();
            return await db.Categories.Where (x => x.ParentId == parentId).ToArrayAsync();
        }
        public static async Task<Items[]> GetItemsByCategory (int categoryId)
        {
            var db = new ShopContext();
            return await db.Items.Where (x => x.CategoryId == categoryId).ToArrayAsync();
        }
        public static async Task<OrderItems[]> GetUserCart (long userId)
        {
            var db = new ShopContext();
            Orders[] orders = await db.Orders.Where (x => x.UserId == userId).ToArrayAsync();
            if ( orders.Length == 0 )
            {
                await CreateOrder (userId);
                orders = await db.Orders.Where (x => x.UserId == userId).ToArrayAsync();
            }
            Orders order = orders.Where (x => x.OrderStatus == (byte) OrderStatus.Cart).First();
            return await db.OrderItems.Where (x => x.OrderId == order.OrderId).ToArrayAsync();
        }
        public static async Task<AdminStatus> GetAdminStatus (long userId)
        {
            var db = new ShopContext();
            Admins? user = await db.Admins.FindAsync (userId);
            return user is not null ? (AdminStatus) user.Status : 0;
        }
        public static async Task EditItemName (int itemId, string name)
        {
            var db = new ShopContext();
            Items? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.ItemName = name;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditItemDesc (int itemId, string desc)
        {
            var db = new ShopContext();
            Items? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.Description = desc;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditItemPrice (int itemId, int price)
        {
            var db = new ShopContext();
            Items? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.Price = price;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditItemImage (int itemId, byte[] image)
        {
            var db = new ShopContext();
            Items? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                item.Image = image;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditCategoryName (int categoryId, string newName)
        {
            var db = new ShopContext();
            Categories? category = await db.Categories.FindAsync (categoryId);
            if ( category is not null
                && category.CategoryName != newName)
            {
                category.CategoryName = newName;
                await db.SaveChangesAsync();
            }
        }
        public static async Task DeleteItem (int itemId)
        {
            var db = new ShopContext();
            Items? item = await db.Items.FindAsync (itemId);
            if ( item is not null )
            {
                db.Items.Remove (item);
                await db.SaveChangesAsync();
            }
        }
        public static async Task DeleteCategory (int categoryId)
        {
            try
            {
                var db = new ShopContext();
                Categories? category = await db.Categories.FindAsync (categoryId);
                if ( category is not null )
                {
                    bool hasCategories = await HasCategories (category.CategoryId);
                    bool hasItems = await HasItems (category.CategoryId);
                    if ( !hasCategories && !hasItems )
                    {
                        db.Categories.Remove (category);
                        await db.SaveChangesAsync();
                    }
                    else if ( hasItems && !hasCategories )
                    {
                        Items[] CategoryItems = await db.Items.Where (x => x.CategoryId == categoryId).ToArrayAsync();
                        db.Items.RemoveRange (CategoryItems);
                        await db.SaveChangesAsync();
                        db.Categories.Remove (category);
                        await db.SaveChangesAsync ();
                    }
                    else if ( hasCategories && !hasItems )
                    {
                        Categories[] ChildCategories = await db.Categories.Where (x => x.ParentId == categoryId).ToArrayAsync();
                        foreach ( Categories c in ChildCategories )
                        {
                            await DeleteCategory (c.CategoryId);
                        }
                        db.Categories.Remove (category);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch ( Exception e )
            {
                Console.WriteLine(e);
            }
        }
        public static async Task AddToCart (long userId, int itemId, byte count)
        {
            var db = new ShopContext();
            OrderItems[] Cart = await GetUserCart (userId);
            if (Cart == null)
                await CreateOrder (userId);
            Orders[] orders = await db.Orders.Where (x => x.UserId == userId).ToArrayAsync();
            Orders order = orders.Where (x => x.OrderStatus == (byte) OrderStatus.Cart).ToArray()[0];
            var ItemToAdd = new OrderItems { OrderId = order.OrderId, Count = count, ItemId = itemId };
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
                await CreateAdmin (userId);
                return await IsAdmin (userId);
            }
            return false;
        }
        public static async Task<bool> ItemExists (int itemId)
        {
            var db = new ShopContext();
            return await db.Items.FindAsync (itemId) is not null;
        }
        public static async Task<bool> CategoryExists (int categoryId)
        {
            var db = new ShopContext();
            return await db.Categories.FindAsync (categoryId) is not null;
        }
        public static async Task<bool> HasCategories (int categoryId)
        {
            var db = new ShopContext();
            return (await db.Categories.Where ( x => x.ParentId == categoryId).ToArrayAsync()).Length > 0;
        }
        public static async Task<bool> HasItems (int categoryId)
        {
            var db = new ShopContext();
            return (await db.Items.Where (x => x.CategoryId == categoryId).ToArrayAsync ()).Length > 0;
        }
        public static async Task<string> GetStringPath (object obj)
        {
            StringBuilder sb = new ();
            Categories category;
            if ( obj is Items )
            {
                category = await GetCategory (((Items) obj).CategoryId);
            }
            else 
                category = (Categories) obj;
            await WriteParent (category);
            return sb.ToString ();
            async Task WriteParent (Categories c)
            {
                if ( c.ParentId != 0 )
                    await WriteParent (await GetCategory (c.ParentId));
                sb.Append (c.CategoryName + " / ");
                return;
            }
        }
        public static async Task SetAdminStatus (long userId, AdminStatus status)
        {
            var db = new ShopContext();
            Admins? admin = await db.Admins.FindAsync (userId);
            if ( admin is not null )
            {
                admin.Status = (byte) status;
                await db.SaveChangesAsync();
            }
        }
    }
}
