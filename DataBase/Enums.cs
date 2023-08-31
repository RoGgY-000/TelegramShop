namespace TelegramShop.DataBase
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public enum AdminStatus : byte
    {
        Clear = 0,
        CreateCategory = 1,
        EditCategory = 2,
        DeleteCategory = 3,
        DeleteItem = 9,
        CreateItemName = 11,
        CreateItemDesc = 12,
        CreateItemImage = 13,
        EditItemName = 21,
        EditItemDesc = 22,
        EditItemPrice = 23,
        EditItemCategory = 24,
        EditItemImage = 25,
    }

    public enum OrderStatus : byte
    {
        Cart,
        Created,
        Confirmed,
        WaitForPay,
        Paid,
        Processing,
        Processed,
        Shipping,
        Shipped,
        Received,
        Completed,
        Cancelled,
        Stopped,
        Returned
    }
}