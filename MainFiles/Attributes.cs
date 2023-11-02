
namespace TelegramShop.Attributes
{
    using TelegramShop.Enums;

    [AttributeUsage (AttributeTargets.Method)]
    class RouteAttribute : Attribute
    {
        public string[] Routes { get; set; }
        public RouteAttribute (params string[] routes) => Routes = routes;
    }
}
