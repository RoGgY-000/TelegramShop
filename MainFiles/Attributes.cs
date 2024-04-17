
namespace TelegramShop.Attributes
{
    using TelegramShop.Enums;

    [AttributeUsage (AttributeTargets.Method)]
    class RouteAttribute : Attribute
    {
        public string[] Routes { get; set; }
        public bool WithoutPermission { get; set; }
        public RouteAttribute ( bool withoutPermission, params string[] routes) => (Routes, WithoutPermission) = (routes, withoutPermission);

        public RouteAttribute (params string[] routes) => (Routes, WithoutPermission) = (routes, false);
    }

    class AccessAttribute : Attribute
    {
        public string[] Permissions { get; set; }
        public AccessAttribute (params string[] permissions) => Permissions = permissions;
    }
}
