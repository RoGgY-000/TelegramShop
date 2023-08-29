
namespace TelegramShop.DataBase
{
    using Microsoft.EntityFrameworkCore;

    using TelegramShop.AES;
    public class ShopContext : DbContext
    {
        public DbSet<Item> Item { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderItem> OrderItem { get; set; }
        public DbSet<Admin> Admin { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                "Host=localhost;" +
                "Port=5432;" +
                "Database=TelegramShop;" +
                "Username=postgres;" +
                $"Password={AESEncoding.GetDBPassword ()}");
        }
    }
}
