
namespace TelegramShop.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum AdminStatus : byte
{
    Clear = 0,
    CreateCategory = 1,
    EditCategory ,
    DeleteCategory,
    CreateItemName = 11,
    CreateItemGlobalPrice,
    CreateItemDesc,
    CreateItemImage,
    EditItemName = 21,
    EditItemDesc,
    EditItemCategory,
    EditItemImage,
    DeleteItem,
    CreateStoreName = 31,
    CreateStoreRegion,
    CreateStoreAdress,
    EditStoreName,
    EditStoreRegion,
    DeleteStore
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