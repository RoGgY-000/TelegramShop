namespace TelegramShop.Controllers
{
    using Telegram.Bot.Types.ReplyMarkups;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using TelegramShop.Routing;
    using TelegramShop.Caching;
    using TelegramShop.Enums;
    using TelegramShop.Keyboards;
    using TelegramShop.Attributes;
    using TelegramShop.DataBase;

    internal class MessageController
    {
        [Route (true, "/start", "menu")]
        public static async Task<(string, InlineKeyboardMarkup)> Start (Update update)
        {
            long userId = Router.GetUserId (update);
            return ("Здравствуйте, это онлайн-магазин в телеграме.", await Kb.Menu (userId));
        }

        [Route ("/admin", "admin")]
        public static async Task<(string, InlineKeyboardMarkup)> Admin (Update update)
        {

            long userId = Router.GetUserId (update);
            return await Db.IsAdmin (userId)
                ? ("Многофункциональная панель администратора", await Kb.Admin (userId))
                : ("Здравствуйте, это магазин в телеграме.", await Kb.Menu (userId));
        }

        [Route ("/clear")]
        public static async Task<(string,InlineKeyboardMarkup)> ClearDB (Update update)
        {
            Db.ClearDB ();
            return ("Cleared", InlineKeyboardMarkup.Empty ());
        }

        [Route ("default")]
        public static async Task<(string, InlineKeyboardMarkup)> DefaultMessageHandler (Update update)
        {
            long userId = Router.GetUserId (update);
            string text = Router.RemoveBadChars (update.Message.Text);
            string replyText = string.Empty;
            InlineKeyboardMarkup ReplyMarkup = InlineKeyboardMarkup.Empty ();

            if ( await Db.IsAdmin (userId)
                && Db.EditCache.ContainsKey (userId) )
                switch ( await Db.GetAdminStatus (userId) )
                {
                    case AdminStatus.CreateCategory:
                        if ( Db.TryGetFromCache (userId, out object? category)
                            && category is Category categoryToCreate )
                            await Db.CreateCategory (text, categoryToCreate.ParentId);
                        replyText = await Db.GetCategoryByName (text) is not null
                            ? $"Категория \"{text}\" успешно создана!"
                            : "Не удалось создать категорию!";
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        await Db.RemoveUser (userId);
                        break;
                    case AdminStatus.EditCategory:
                        if ( Db.TryGetFromCache (userId, out object? category1)
                            && category1 is Category categoryToEdit
                            && Db.GetCategory (categoryToEdit.CategoryId) is not null )
                        {
                            await Db.EditCategoryName (categoryToEdit.CategoryId, text);
                            replyText = (await Db.GetCategory (categoryToEdit.CategoryId)).CategoryName == text
                                ? "Название категории успешно изменено." : "failed";
                            await Db.RemoveUser (userId);
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        }
                        break;
                    case AdminStatus.DeleteCategory:
                        if ( Db.TryGetFromCache (userId, out object? category2)
                            && category2 is Category categoryToDelete
                            && text == categoryToDelete.CategoryName )
                        {
                            await Db.DeleteCategory (categoryToDelete.CategoryId);
                            replyText = !await Db.CategoryExists (categoryToDelete.CategoryId)
                                ? $"Категория \"{categoryToDelete.CategoryName}\" и все категории и товары в ней успешно удалены"
                                : $"Не удалось удалить категорию \"{categoryToDelete.CategoryName}\"!";
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                            await Db.RemoveUser (userId);
                        }
                        else
                            replyText = "Название не совпадает!";
                        break;
                    case AdminStatus.DeleteItem:
                        if ( Db.TryGetFromCache (userId, out object? item)
                            && item is Item itemToDelete
                            && text == (await Db.GetItem (itemToDelete.ItemId)).ItemName )
                        {
                            await Db.DeleteItem (itemToDelete.ItemId);
                            replyText = !await Db.ItemExists (itemToDelete.ItemId)
                                ? $"Товар \"{itemToDelete.ItemName}\" успешно удалён"
                                : $"Не удалось удалить товар \"{itemToDelete.ItemName}\"!";
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                            await Db.RemoveUser (userId);
                        }
                        else
                            replyText = "Название не совпадает!";
                        break;
                    case AdminStatus.CreateItemName:
                        if ( Db.TryGetFromCache (userId, out object? item1)
                           && item1 is Item itemToCreate )
                        {
                            itemToCreate.ItemName = text ?? string.Empty;
                            await Db.SetAdminStatus (userId, AdminStatus.CreateItemDesc);
                            replyText = "Введите описание товара (введите '_', чтобы оставить пустым):";
                        }
                        break;
                    case AdminStatus.CreateItemDesc:
                        if ( Db.TryGetFromCache (userId, out object? item3)
                            && item3 is Item itemToCreate3 )
                        {
                            itemToCreate3.Description = (text == "_") ? null : text;
                            await Db.SetAdminStatus (userId, AdminStatus.CreateItemGlobalPrice);
                            replyText = "Введите стартовую (глобальную) цену товара:";
                        }
                        break;
                    case AdminStatus.CreateItemGlobalPrice:
                        if ( int.TryParse (text, out int newPrice)
                            && Db.TryGetFromCache (userId, out object? item2)
                            && item2 is Item itemToCreate2 )
                        {
                            await Db.SetAdminStatus (userId, AdminStatus.CreateItemImage);
                            itemToCreate2 = await Db.CreateItem (itemToCreate2);
                            if ( await Db.ItemExists (itemToCreate2.ItemId) )
                            {
                                await Db.CreateStoreItem (new StoreItem { ItemId = itemToCreate2.ItemId, Price = newPrice, StoreId = Store.Default.StoreId });
                                replyText = $"Товар {itemToCreate2.ItemName} успешно создан";
                                ReplyMarkup = await Kb.Back ($"edit_item?id={itemToCreate2.ItemId:d10}");
                            }
                        }
                        break;
                    case AdminStatus.CreateItemImage:
                        if ( update.Message.Photo is null )
                        {
                            var stream = new MemoryStream ();
                            //    Console.WriteLine (message.Photo.Last ().FileId);
                            if ( Db.TryGetFromCache (userId, out object? item4)
                                && item4 is Item itemToCreate4 )
                            {
                                await Db.CreateItem (itemToCreate4);
                                if ( await Db.ItemExists (itemToCreate4.ItemId) )
                                {
                                    replyText = $"Товар {itemToCreate4.ItemName} успешно создан";
                                    ReplyMarkup = await Kb.Back ($"edit_item?id={itemToCreate4.ItemId:d10}");
                                }
                            }
                        }
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        await Db.RemoveUser (userId);
                        break;
                    case AdminStatus.EditItemName:
                        if ( Db.TryGetFromCache (userId, out object? item5)
                            && item5 is Item itemToEdit
                            && await Db.ItemExists (itemToEdit.ItemId) )
                        {
                            await Db.EditItemName (itemToEdit.ItemId, text);
                            if ( (await Db.GetItem (itemToEdit.ItemId)).ItemName == text )
                                replyText = "Название товара успешно изменено.";
                        }
                        await Db.RemoveUser (userId);
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        break;
                    case AdminStatus.EditItemDesc:
                        if ( Db.TryGetFromCache (userId, out object? item6)
                            && item6 is Item itemToEdit1
                            && await Db.ItemExists (itemToEdit1.ItemId) )
                        {
                            await Db.EditItemDesc (itemToEdit1.ItemId, text);
                            if ( (await Db.GetItem (itemToEdit1.ItemId)).Description == text )
                                replyText = "Описание товара успешно изменено.";
                        }
                        await Db.RemoveUser (userId);
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        break;
                    case AdminStatus.CreateStoreName:
                        if ( Db.TryGetFromCache (userId, out object? store)
                            && store is Store storeToCreate )
                        {
                            storeToCreate.StoreName = text;
                            await Db.SetAdminStatus (userId, AdminStatus.CreateStoreRegion);
                            replyText = "Введите регион, в котором расположен магазин:";
                        }
                        break;
                    case AdminStatus.CreateStoreRegion:
                        if ( Db.TryGetFromCache (userId, out object? store1)
                            && store1 is Store storeToCreate1 )
                        {
                            storeToCreate1.Region = text;
                            await Db.SetAdminStatus (userId, AdminStatus.CreateStoreAdress);
                            replyText = "Введите адрес магазина:";
                        }
                        break;
                    case AdminStatus.CreateStoreAdress:
                        if ( Db.TryGetFromCache (userId, out object? store2)
                            && store2 is Store storeToCreate2 )
                        {
                            storeToCreate2.Adress = text;
                            storeToCreate2 = await Db.CreateStore (storeToCreate2);
                            await Db.AddToCache (userId, storeToCreate2);
                            if ( await Db.StoreExists (storeToCreate2.StoreId) )
                                replyText = "Магазин успешно создан!";
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                            await Db.RemoveUser (userId);
                            ReplyMarkup = await Kb.Back ($"edit_store?id={storeToCreate2.StoreId:d10}");
                        }
                        break;
                    case AdminStatus.EditStoreName:
                        if ( Db.TryGetFromCache (userId, out object? store3)
                            && store3 is Store storeToEdit
                            && await Db.StoreExists (storeToEdit.StoreId) )
                        {
                            await Db.EditStoreName (storeToEdit.StoreId, text);
                            if ( (await Db.GetStore (storeToEdit.StoreId)).StoreName == text )
                                replyText = "Название магазина успешно изменено.";
                        }
                        await Db.RemoveUser (userId);
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        break;
                    case AdminStatus.EditStoreRegion:
                        if ( Db.TryGetFromCache (userId, out object? store4)
                            && store4 is Store storeToEdit1
                            && await Db.StoreExists (storeToEdit1.StoreId) )
                        {
                            await Db.EditStoreRegion (storeToEdit1.StoreId, text);
                            if ( (await Db.GetStore (storeToEdit1.StoreId)).Region == text )
                                replyText = "Регион магазина успешно изменён.";
                        }
                        await Db.RemoveUser (userId);
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        break;
                    case AdminStatus.DeleteStore:
                        if ( Db.TryGetFromCache (userId, out object? store5)
                            && store5 is Store storeToDelete
                            && await Db.StoreExists (storeToDelete.StoreId)
                            && text == (await Db.GetStore (storeToDelete.StoreId)).StoreName )
                        {
                            await Db.DeleteStore (storeToDelete.StoreId);
                            replyText = !await Db.StoreExists (storeToDelete.StoreId)
                                ? $"Магазин \"{storeToDelete.StoreName}\" успешно удалён"
                                : $"Не удалось удалить магазин \"{storeToDelete.StoreName}\"!";

                        }
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        await Db.RemoveUser (userId);
                        break;
                    case AdminStatus.CreateItemLocalPrice:
                        if ( int.TryParse (text, out int price)
                            && Db.TryGetFromCache (userId, out object obj)
                            && obj is StoreItem si )
                        {
                            await Db.SetAdminStatus (userId, AdminStatus.CreateItemLocalCount);
                            si.Price = price;
                            Db.EditCache.UpdateValue (userId, si);
                            replyText = "Введите количество товара в этом магазине";
                            ReplyMarkup = InlineKeyboardMarkup.Empty ();
                        }
                        break;
                    case AdminStatus.CreateItemLocalCount:
                        if ( int.TryParse (text, out int count)
                            && Db.TryGetFromCache (userId, out object obj1)
                            && obj1 is StoreItem si1 )
                        {
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                            si1.Count = count;
                            await Db.CreateStoreItem (si1);
                            await Db.RemoveUser (userId);
                            replyText = "Цена успешно установлена";
                            ReplyMarkup = await BtnGenerator.GetOneButtonMarkup ("Назад", $"edit_item_prices?id={si1.ItemId:d10}");
                        }
                        break;
                    case AdminStatus.EditStoreItemPrice:
                        if ( int.TryParse (text, out int price1)
                            && Db.TryGetFromCache (userId, out object obj2)
                            && obj2 is StoreItem si2 )
                        {
                            await Db.EditStoreItemPrice (si2.Id, price1);
                            await Db.RemoveUser (userId);
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                            replyText = "Цена успешно изменена";
                            ReplyMarkup = await BtnGenerator.GetOneButtonMarkup ("Назад", $"edit_item_price?id={si2.Id:d10}");
                        }
                        break;
                    case AdminStatus.EditStoreItemCount:
                        if ( int.TryParse (text, out int count1)
                            && Db.TryGetFromCache (userId, out object obj3)
                            && obj3 is StoreItem si3 )
                        {
                            await Db.EditStoreItemCount (si3.Id, count1);
                            await Db.RemoveUser (userId);
                            await Db.SetAdminStatus (userId, AdminStatus.Clear);
                            replyText = "Количество успешно изменено";
                            ReplyMarkup = await BtnGenerator.GetOneButtonMarkup ("Назад", $"edit_item_price?id={si3.Id:d10}");
                        }
                        break;
                    case AdminStatus.CreateRoleName:
                        Role r = new () { RoleName = text };
                        await Db.SetAdminStatus (userId, AdminStatus.CreateRoleDescription);
                        await Db.AddToCache (userId, r);
                        replyText = "Введите описание роли:";
                        break;
                    case AdminStatus.CreateRoleDescription:
                        if ( Db.TryGetFromCache (userId, out object obj4)
                            && obj4 is Role r1 )
                        {
                            r1.Description = text;
                            await Db.SetAdminStatus (userId, AdminStatus.CreateRoleLevel);
                            replyText = "Введите уровень роли:\n(От 0 до 255 включительно, чем больше тем выше)";
                        }
                        break;
                    case AdminStatus.CreateRoleLevel:
                        if ( byte.TryParse (text, out byte level)
                            && Db.TryGetFromCache (userId, out object obj5)
                            && obj5 is Role r2 )
                        {
                            r2.Level = level;
                            await Db.SetAdminStatus (userId, AdminStatus.CreateRolePermissions);
                            Role role = await Db.CreateRole (r2);
                            ReplyMarkup = await Kb.CreateRolePermissions (r2.RoleId);
                            replyText = "Выберите разрешения для этой роли:";
                        }
                        break;
                    default: replyText = "Unknown text!"; break;
                }
            else if ( int.TryParse (text, out int count)
                && Db.SelectCountCache.TryGetValue (userId, out object? item) )
            {
                if ( item is Item i )
                {
                    await Db.AddToCart (userId, i.ItemId, count);
                    Db.SelectCountCache.RemovePair (userId);
                    (replyText, ReplyMarkup) = await CallbackQueryController.Cart (update);
                }
                else if ( item is OrderItem oi )
                {
                    await Db.EditOrderItemCount (oi.Id, count);
                    await Db.RemoveUser (userId);
                    (replyText, ReplyMarkup) = await CallbackQueryController.Cart (update);
                }
            }
            else
                replyText = "Unknown text!";
            return (replyText, ReplyMarkup);
        }
    }
}
