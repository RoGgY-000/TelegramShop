
namespace TelegramShop.Attributes
{
    using TelegramShop.Enums;

    [AttributeUsage (AttributeTargets.Method)]
    class RouteAttribute : Attribute
    {
        public string[] Routes { get; set; }
        public RouteAttribute (params string[] routes) => Routes = routes;
    }

    class AccessAttribute : Attribute
    {
        public string[] Permissions { get; set; }
        public AccessAttribute (params string[] permissions) => Permissions = permissions;
    }
}
