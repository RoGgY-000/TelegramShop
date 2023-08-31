﻿
namespace TelegramShop.DataBase
{
    using System.ComponentModel.DataAnnotations;

    public partial class Item
    {
        [Key]
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public byte[]? Image { get; set; }
    }

    public partial class Price
    {
        [Key]
        public int PriceId { get; set; }
        public int ItemId { get; set; }
        public int RegionId { get; set; }
        public int PriceValue { get; set; }
    }

    public partial class Region
    {
        [Key]
        public int RegionId { get; set; }
        public string RegionName { get; set; }
    }

    public partial class Property
    {
        [Key]
        public int PropertyId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public partial class IntProperty
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int PropertyId { get; set; }
        public int PropertyValue { get; set; }
    }

    public partial class StringProperty
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyValue { get; set; }
    }

    public partial class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ParentId { get; set; }
        public int Weight { get; set; }
    }

    public partial class Order
    {
        [Key]
        public int OrderId { get; set; }
        public long UserId { get; set; }
        public DateTime OrderDateTime { get; set; }
        public int Summ { get; set; }
        public byte OrderStatus { get; set; }
    }

    public partial class OrderItem
    {
        [Key]
        public int OrderId { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
        public int ItemPriceId { get; set; }
    }

    public partial class Admin
    {
        [Key]
        public long UserId { get; set; }
        public byte Status { get; set; }
        public int Role { get; set; }
    }

    public partial class Role
    {
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string? Description { get; set; }
        public byte Level { get; set; }
    }

    public partial class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        public string PermissionName { get; set; }
        public string? Description { get; set; }
    }

    public partial class RolePermission
    {
        [Key]
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }
}