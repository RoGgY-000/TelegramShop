
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
    CreateItemLocalPrice,
    CreateItemLocalCount,
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
    DeleteStore,
    EditStoreItemPrice = 41,
    EditStoreItemCount,
    CreateRoleName = 51,
    CreateRoleDescription,
    CreateRoleLevel,
    CreateRolePermissions,
    EditRoleName,
    EditRoleDescription,
    EditRoleLevel,
    EditRolePermissions,
    DeleteRole
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