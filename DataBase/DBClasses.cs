
namespace TelegramShop.DataBase
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Item
    {
        [Key]
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public byte[]? Image { get; set; }

        [ForeignKey ("CategoryId")]
        public Category Category { get; set; }
    }

    public class StoreItem
    {
        [Key]
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int ItemId { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }

        [ForeignKey ("StoreId")]
        public Store Store { get; set; }

        [ForeignKey ("ItemId")]
        public Item Item { get; set; }
    }

    public class Store
    {
        [Key]
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string City { get; set; }
        public string? Adress { get; set; }

        public static Store Default = new () { StoreId = 0, StoreName = "Default", City = "_" };
}

    public class Property
    {
        [Key]
        public int PropertyId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public PropertyType Type { get; set; }
    }

    public class IntProperty
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int PropertyId { get; set; }
        public int PropertyValue { get; set; }

        [ForeignKey ("ItemId")]
        public Item Item { get; set; }

        [ForeignKey ("PropertyId")]
        public Property Property { get; set; }
    }

    public class StringProperty
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyValue { get; set; }

        [ForeignKey ("ItemId")]
        public Item Item { get; set; }

        [ForeignKey ("PropertyId")]
        public Property Property { get; set; }
    }

    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ParentId { get; set; }
        public int Weight { get; set; }

        [ForeignKey ("ParentId")]
        public Category? Parent { get; set; }
    }

    public class Order
    {
        [Key]
        public int OrderId { get; set; }
        public long UserId { get; set; }
        public DateTime OrderDateTime { get; set; }
        public int Summ { get; set; }
        public OrderStatus OrderStatus { get; set; }
    }

    public class OrderItem
    {
        [Key]
        public int OrderId { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
        public int Price { get; set; }

        [ForeignKey ("OrderId")]
        public Order Order { get; set; }

        [ForeignKey ("ItemId")]
        public Item Item { get; set; }
    }

    public class Admin
    {
        [Key]
        public long UserId { get; set; }
        public AdminStatus Status { get; set; }
        public int RoleId { get; set; }

        [ForeignKey ("RoleId")]
        public Role Role { get; set; }
    }

    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string? Description { get; set; }
        public byte Level { get; set; }

        public static Role Creator = new () { RoleId = 0, RoleName = "Creator", Level = 255 };
    }

    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        public string PermissionName { get; set; }
        public string? Description { get; set; }
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }

        [ForeignKey ("RoleId")]
        public Role Role { get; set; }

        [ForeignKey ("PermissionId")]
        public Permission Permission { get; set; }
    }
}