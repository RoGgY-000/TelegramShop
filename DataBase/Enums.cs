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
        CreateItemName = 11,
        CreateItemDesc = 12,
        CreateItemPrice = 13,
        CreateItemImage = 14,
        EditItemName = 15,
        EditItemDesc = 16,
        EditItemPrice = 17,
        EditItemCategory = 18,
        EditItemImage = 19,
        DeleteItem = 20
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