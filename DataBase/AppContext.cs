
namespace TelegramShop.DataBase
{
    using Microsoft.EntityFrameworkCore;

    using TelegramShop.AES;
    public class ShopContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Price> Prices { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreItemCount> StoreItemCounts { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<IntProperty> IntProperties { get; set; }
        public DbSet<StringProperty> StringProperties { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                "Host=localhost;" +
                "Port=5432;" +
                "Database=TelegramShop;" +
                "Username=postgres;" +
                $"Password={AESEncoding.GetDBPassword ()}");
            optionsBuilder.LogTo (Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Warning);
        }
    }
}
