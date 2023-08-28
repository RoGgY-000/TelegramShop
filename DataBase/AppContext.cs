
namespace TelegramShop.DataBase
{
    using Microsoft.EntityFrameworkCore;

    using TelegramShop.AES;
    public class ShopContext : DbContext
    {
        public DbSet<Items> Items { get; set; }
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }
        public DbSet<Admins> Admins { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                "Host=localhost;" +
                "Port=5432;" +
                "Database=TelegramShop;" +
                "Username=postgres;" +
                "Password=geGtQnHmoW");
        }
    }
}
