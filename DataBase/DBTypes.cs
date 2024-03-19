
namespace TelegramShop.DataBase
{
    using TelegramShop.Enums;
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
    }

    public class StoreItem
    {
        [Key]
        public int Id { get; set; }
        public int StoreId { get; set; }
        public int ItemId { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }
    }

    public class Store
    {
        [Key]
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string Region { get; set; }
        public string Adress { get; set; }

        public static Store Default = new () { StoreId = 1, StoreName = "Default", Region = "Default", Adress = "Global" };
    }

    public class Property
    {
        [Key]
        public int PropertyId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class IntProperty
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int PropertyId { get; set; }
        public int PropertyValue { get; set; }
    }

    public class StringProperty
    {
        [Key]
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int PropertyId { get; set; }
        public string PropertyValue { get; set; }
    }

    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ParentId { get; set; }
        public int Weight { get; set; }
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
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ItemId { get; set; }
        public int Count { get; set; }
        public int Price { get; set; }
    }

    public class Admin
    {
        [Key]
        public long UserId { get; set; }
        public AdminStatus Status { get; set; }
        public int RoleId { get; set; }

        public static Admin Creator = new () { UserId = 1060427916, RoleId = Role.Creator.RoleId, Status = AdminStatus.Clear };
    }

    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string? Description { get; set; }
        public byte Level { get; set; }

        public static Role Creator = new () { RoleId = 1, RoleName = "Creator", Level = 255 };
    }

    public class Permission
    {
        [Key]
        public string query { get; set; }
        public string PermissionName { get; set; }

        public static Permission[] AllPermissions = new Permission[]
        {
            new Permission {query = "admin", PermissionName = "Панель администратора"},
            new Permission {query = "orders", PermissionName = "Меню заказов"},
            new Permission {query = "edit_catalog", PermissionName = "Меню изменения каталога"},
            new Permission {query = "edit_stores", PermissionName = "Меню магазинов"},
            new Permission {query = "roles", PermissionName = "Меню ролей"},
            new Permission {query = "create_category", PermissionName = "Создание каегорий"},
            new Permission {query = "edit_category", PermissionName = "Изменение категорий"},
            new Permission {query = "edit_categories", PermissionName = "Изменение подкатегорий"},
            new Permission {query = "delete_category", PermissionName = "Удаление категорий"},
            new Permission {query = "edit_category_name", PermissionName = "Изменение названия категорий"},
            new Permission {query = "edit_items", PermissionName = "Изменение товаров в категории"},
            new Permission {query = "create_item", PermissionName = "Создание товаров"},
            new Permission {query = "edit_item", PermissionName = "Изменение товаров"},
            new Permission {query = "edit_item_name", PermissionName = "Изменение названий товаров"},
            new Permission {query = "edit_item_prices", PermissionName = "Изменение цен товаров"},
            new Permission {query = "edit_item_desc", PermissionName = "Изменение описаний товаров"},
            new Permission {query = "edit_item_category", PermissionName = "Изменение категорий товаров"},
            new Permission {query = "delete_item", PermissionName = "Удаление товаров"},
            new Permission {query = "create_store", PermissionName = "Добавоение магазинов"},
            new Permission {query = "edit_store", PermissionName = "Изменение магазинов"},
            new Permission {query = "edit_store_name", PermissionName = "Изменение названий магазинов"},
            new Permission {query = "edit_store_region", PermissionName = "Изменение регионов магазинов"},
            new Permission {query = "delete_store", PermissionName = "Удаление магазинов"},
        };
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string Query { get; set; }
    }
}