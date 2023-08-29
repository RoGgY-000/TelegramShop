
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
            Item[] CurrentItems;
            do 
            {
                randomId = random.Next (int.MaxValue);
                CurrentItems = await db.Item.Where (x => x.ItemId == randomId).ToArrayAsync();
            } while ( CurrentItems.Length > 0 );
            var newItem = new Item { ItemId = randomId, CategoryId = categoryId, ItemName = name, Description = desc, ItemPriceId = price };
            await db.Item.AddAsync (newItem);
            await db.SaveChangesAsync();
        }
        public static async Task CreateCategory (string categoryName, int parentId)
        {
            var db = new ShopContext();
            var random = new Random();
            int randomId;
            Category[] CurrentCategories;
            do
            {
                randomId = random.Next (int.MaxValue);
                CurrentCategories = await db.Category.Where (x => x.CategoryId == randomId).ToArrayAsync();
            } while ( CurrentCategories.Length > 0 );
            var newCategory = new Category { CategoryId = randomId, CategoryName = categoryName, ParentId = parentId };
            await db.Category.AddAsync (newCategory);
            await db.SaveChangesAsync ();
        }
        public static async Task CreateOrder (long userId)
        {
            var db = new ShopContext();
            var random = new Random();
            int randomId;
            Order[] CurrentOrders;
            do
            {
                randomId = random.Next (int.MaxValue);
                CurrentOrders = await db.Order.Where (x => x.OrderId == randomId).ToArrayAsync();
            } while ( CurrentOrders.Length > 0 );
            var order = new Order { OrderId = randomId, UserId = userId, OrderStatus = (byte) OrderStatus.Cart};
            await db.Order.AddAsync (order);
            await db.SaveChangesAsync ();
        }
        public static async Task CreateAdmin (long userId)
        {
            var db = new ShopContext();
            var newAdmin = new Admin { UserId = userId, Status = 0 };
            await db.Admin.AddAsync (newAdmin);
            await db.SaveChangesAsync();
        }
        public static async Task<Item> GetItem (int itemId)
        {
            var db = new ShopContext();
            return await db.Item.Where ( x => x.ItemId == itemId).FirstAsync();
        }
        public static async Task<int> GetItemCountInCategory (int categoryId)
        {
            var db = new ShopContext();
            Category? category = await db.Category.FindAsync (categoryId);
            return category is not null ? await db.Item.Where (x => x.CategoryId == categoryId).CountAsync() : 0;
        }
        public static async Task<Category> GetCategory (int categoryId)
        {
            var db = new ShopContext();
            return await db.Category.Where (x => x.CategoryId == categoryId).FirstAsync();
        }
        public static async Task<Category> GetCategoryByName (string categoryName)
        {
            var db = new ShopContext();
            return await db.Category.Where (x => x.CategoryName == categoryName).FirstAsync();

        }
        public static async Task<Category[]> GetRootCategories()
        {
            var db = new ShopContext();
            return await db.Category.Where (x => x.ParentId == 0).ToArrayAsync();
        }
        public static async Task<Category[]> GetChildCategories (int parentId)
        {
            var db = new ShopContext();
            return await db.Category.Where (x => x.ParentId == parentId).ToArrayAsync();
        }
        public static async Task<Item[]> GetItemsByCategory (int categoryId)
        {
            var db = new ShopContext();
            return await db.Item.Where (x => x.CategoryId == categoryId).ToArrayAsync();
        }
        public static async Task<OrderItem[]> GetUserCart (long userId)
        {
            var db = new ShopContext();
            Order[] orders = await db.Order.Where (x => x.UserId == userId).ToArrayAsync();
            if ( orders.Length == 0 )
            {
                await CreateOrder (userId);
                orders = await db.Order.Where (x => x.UserId == userId).ToArrayAsync();
            }
            Order order = orders.Where (x => x.OrderStatus == (byte) OrderStatus.Cart).First();
            return await db.OrderItem.Where (x => x.OrderId == order.OrderId).ToArrayAsync();
        }
        public static async Task<AdminStatus> GetAdminStatus (long userId)
        {
            var db = new ShopContext();
            Admin? user = await db.Admin.FindAsync (userId);
            return user is not null ? (AdminStatus) user.Status : 0;
        }
        public static async Task EditItemName (int itemId, string name)
        {
            var db = new ShopContext();
            Item? item = await db.Item.FindAsync (itemId);
            if ( item is not null )
            {
                item.ItemName = name;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditItemDesc (int itemId, string desc)
        {
            var db = new ShopContext();
            Item? item = await db.Item.FindAsync (itemId);
            if ( item is not null )
            {
                item.Description = desc;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditItemPrice (int itemId, int price)
        {
            var db = new ShopContext();
            Item? item = await db.Item.FindAsync (itemId);
            if ( item is not null )
            {
                item.ItemPriceId = price;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditItemImage (int itemId, byte[] image)
        {
            var db = new ShopContext();
            Item? item = await db.Item.FindAsync (itemId);
            if ( item is not null )
            {
                item.Image = image;
                await db.SaveChangesAsync();
            }
        }
        public static async Task EditCategoryName (int categoryId, string newName)
        {
            var db = new ShopContext();
            Category? category = await db.Category.FindAsync (categoryId);
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
            Item? item = await db.Item.FindAsync (itemId);
            if ( item is not null )
            {
                db.Item.Remove (item);
                await db.SaveChangesAsync();
            }
        }
        public static async Task DeleteCategory (int categoryId)
        {
            try
            {
                var db = new ShopContext();
                Category? category = await db.Category.FindAsync (categoryId);
                if ( category is not null )
                {
                    bool hasCategories = await HasCategories (category.CategoryId);
                    bool hasItems = await HasItems (category.CategoryId);
                    if ( !hasCategories && !hasItems )
                    {
                        db.Category.Remove (category);
                        await db.SaveChangesAsync();
                    }
                    else if ( hasItems && !hasCategories )
                    {
                        Item[] CategoryItems = await db.Item.Where (x => x.CategoryId == categoryId).ToArrayAsync();
                        db.Item.RemoveRange (CategoryItems);
                        await db.SaveChangesAsync();
                        db.Category.Remove (category);
                        await db.SaveChangesAsync ();
                    }
                    else if ( hasCategories && !hasItems )
                    {
                        Category[] ChildCategories = await db.Category.Where (x => x.ParentId == categoryId).ToArrayAsync();
                        foreach ( Category c in ChildCategories )
                        {
                            await DeleteCategory (c.CategoryId);
                        }
                        db.Category.Remove (category);
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
            OrderItem[] Cart = await GetUserCart (userId);
            if (Cart == null)
                await CreateOrder (userId);
            Order[] orders = await db.Order.Where (x => x.UserId == userId).ToArrayAsync();
            Order order = orders.Where (x => x.OrderStatus == (byte) OrderStatus.Cart).ToArray()[0];
            var ItemToAdd = new OrderItem { OrderId = order.OrderId, Count = count, ItemId = itemId };
            await db.OrderItem.AddAsync (ItemToAdd);
            await db.SaveChangesAsync();
        }
        public static async Task<bool> IsAdmin (long userId)
        {
            var db = new ShopContext();
            if ( await db.Admin.FindAsync (userId) != null )
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
            return await db.Item.FindAsync (itemId) is not null;
        }
        public static async Task<bool> CategoryExists (int categoryId)
        {
            var db = new ShopContext();
            return await db.Category.FindAsync (categoryId) is not null;
        }
        public static async Task<bool> HasCategories (int categoryId)
        {
            var db = new ShopContext();
            return (await db.Category.Where ( x => x.ParentId == categoryId).ToArrayAsync()).Length > 0;
        }
        public static async Task<bool> HasItems (int categoryId)
        {
            var db = new ShopContext();
            return (await db.Item.Where (x => x.CategoryId == categoryId).ToArrayAsync ()).Length > 0;
        }
        public static async Task<string> GetStringPath (object obj)
        {
            StringBuilder sb = new ();
            Category category;
            if ( obj is Item )
            {
                category = await GetCategory (((Item) obj).CategoryId);
            }
            else 
                category = (Category) obj;
            await WriteParent (category);
            return sb.ToString ();
            async Task WriteParent (Category c)
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
            Admin? admin = await db.Admin.FindAsync (userId);
            if ( admin is not null )
            {
                admin.Status = (byte) status;
                await db.SaveChangesAsync();
            }
        }
    }
}
