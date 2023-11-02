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
        [Route ("/start")]
        public static async Task<(string, InlineKeyboardMarkup)> Start (Update update)
        {
            long userId = Router.GetUserId (update);
            return ("Здравствуйте, это магазин в телеграме.", await Kb.Menu (userId));
        }

        [Route ("/admin", "admin")]
        public static async Task<(string, InlineKeyboardMarkup)> Admin (Update update)
        {
            long userId = Router.GetUserId (update);
            return await Db.IsAdmin (userId)
                ? ((string, InlineKeyboardMarkup)) ("Многофункциональная панель администратора", Kb.Admin)
                : ("Здравствуйте, это магазин в телеграме.", await Kb.Menu (userId));
        }

        [Route ("default")]
        public static async Task<(string, InlineKeyboardMarkup)> DefaultMessageHandler (Update update)
        {
            long userId = Router.GetUserId (update);
            string text = Router.RemoveBadChars (update.Message.Text);
            string replyText = string.Empty;
            InlineKeyboardMarkup ReplyMarkup = InlineKeyboardMarkup.Empty ();

            switch ( await Db.GetAdminStatus (userId) )
            {
                case AdminStatus.CreateCategory:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? category)
                        && category is not null
                        && category is Category categoryToCreate )
                        await Db.CreateCategory (text, categoryToCreate.ParentId);
                    replyText = await Db.GetCategoryByName (text) is not null
                        ? $"Категория \"{text}\" успешно создана!"
                        : "Не удалось создать категорию!";
                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
                    break;
                case AdminStatus.EditCategory:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? category1)
                        && category1 is not null
                        && category1 is Category categoryToEdit
                        && Db.GetCategory (categoryToEdit.CategoryId) is not null )
                    {
                        await Db.EditCategoryName (categoryToEdit.CategoryId, text);
                        replyText = (await Db.GetCategory (categoryToEdit.CategoryId)).CategoryName == text
                            ? "Название категории успешно изменено." : "failed";
                        Cache.RemoveUser (userId);
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                    }
                    break;
                case AdminStatus.DeleteCategory:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? category2)
                        && category2 is not null
                        && category2 is Category categoryToDelete
                        && text == (await Db.GetCategory (categoryToDelete.CategoryId)).CategoryName )
                    {
                        await Db.DeleteCategory (categoryToDelete.CategoryId);
                        replyText = !await Db.CategoryExists (categoryToDelete.CategoryId)
                            ? $"Категория \"{categoryToDelete.CategoryName}\" и все категории и товары в ней успешно удалены"
                            : $"Не удалось удалить категорию \"{categoryToDelete.CategoryName}\"!";
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        Cache.RemoveUser (userId);
                    }
                    else
                        replyText = "Название не совпадает!";
                    break;
                case AdminStatus.DeleteItem:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? item)
                        && item is not null
                        && item is Item itemToDelete
                        && text == (await Db.GetItem (itemToDelete.ItemId)).ItemName )
                    {
                        await Db.DeleteItem (itemToDelete.ItemId);
                        replyText = !await Db.ItemExists (itemToDelete.ItemId)
                            ? $"Товар \"{itemToDelete.ItemName}\" успешно удалён"
                            : $"Не удалось удалить товар \"{itemToDelete.ItemName}\"!";
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        Cache.RemoveUser (userId);
                    }
                    else
                        replyText = "Название не совпадает!";
                    break;
                case AdminStatus.CreateItemName:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? item1)
                       && item1 is Item itemToCreate )
                    {
                        itemToCreate.ItemName = text ?? string.Empty;
                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemGlobalPrice);
                        replyText = "Введите стартовую (глобальную) цену товара:";
                    }
                    break;
                case AdminStatus.CreateItemGlobalPrice:
                    if ( int.TryParse (text, out int newPrice)
                        && Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? item2)
                        && item2 is not null
                        && item2 is Item itemToCreate2 )
                    {
                        var globalPrice = new StoreItem { ItemId = itemToCreate2.ItemId, Price = newPrice, StoreId = 0 };
                        await Db.CreateStoreItem (globalPrice);
                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemDesc);
                        replyText = "Введите описание товара (введите '_', чтобы оставить пустым):";
                    }
                    break;
                case AdminStatus.CreateItemDesc:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? item3)
                        && item3 is not null
                        && item3 is Item itemToCreate3 )
                    {
                        itemToCreate3.Description = (text == "_") ? null : text;
                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemImage);
                        replyText = "Отправьте карточку товара:";
                    }
                    break;
                case AdminStatus.CreateItemImage:
                    if ( update.Message.Photo is null )
                    {
                        var stream = new MemoryStream ();
                        //    Console.WriteLine (message.Photo.Last ().FileId);
                        if ( Cache.ContainsKey (userId)
                            && Cache.TryGetValue (userId, out object? item4)
                            && item4 is not null
                            && item4 is Item itemToCreate4 )
                        {
                            await Db.CreateItem (itemToCreate4);
                            if ( await Db.ItemExists (itemToCreate4.ItemId) )
                            {
                                replyText = $"Товар {itemToCreate4.ItemName} успешно создан";
                                ReplyMarkup = await Kb.Back ($"edit_item {itemToCreate4.ItemId:d10}");
                            }
                        }
                    }
                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
                    Cache.RemoveUser (userId);
                    break;
                case AdminStatus.EditItemName:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? item5)
                        && item5 is not null
                        && item5 is Item itemToEdit
                        && await Db.ItemExists (itemToEdit.ItemId) )
                    {
                        await Db.EditItemName (itemToEdit.ItemId, text);
                        if ( (await Db.GetItem (itemToEdit.ItemId)).ItemName == text )
                            replyText = "Название товара успешно изменено.";
                    }
                    Cache.RemoveUser (userId);
                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
                    break;
                case AdminStatus.EditItemDesc:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? item6)
                        && item6 is not null
                        && item6 is Item itemToEdit1
                        && await Db.ItemExists (itemToEdit1.ItemId) )
                    {
                        await Db.EditItemDesc (itemToEdit1.ItemId, text);
                        if ( (await Db.GetItem (itemToEdit1.ItemId)).Description == text )
                            replyText = "Описание товара успешно изменено.";
                    }
                    Cache.RemoveUser (userId);
                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
                    break;
                case AdminStatus.CreateStoreName:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? store)
                        && store is not null
                        && store is Store storeToCreate )
                    {
                        storeToCreate.StoreName = text;
                        await Db.SetAdminStatus (userId, AdminStatus.CreateStoreCity);
                        replyText = "Введите город, в котором расположен магазин:";
                    }
                    break;
                case AdminStatus.CreateStoreCity:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? store1)
                        && store1 is not null
                        && store1 is Store storeToCreate1 )
                    {
                        storeToCreate1.City = text;
                        await Db.SetAdminStatus (userId, AdminStatus.CreateStoreAdress);
                        replyText = "Введите адрес магазина:";
                    }
                    break;
                case AdminStatus.CreateStoreAdress:
                    if ( Cache.ContainsKey (userId)
                        && Cache.TryGetValue (userId, out object? store2)
                        && store2 is not null
                        && store2 is Store storeToCreate2 )
                    {
                        storeToCreate2.Adress = text;
                        await Db.CreateStore (storeToCreate2);
                        if ( await Db.StoreExists (storeToCreate2.StoreId) )
                            replyText = "Магазин успешно создан!";
                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
                        ReplyMarkup = await Kb.Back ($"edit_store {storeToCreate2.StoreId:d10}");
                    }
                    break;
                default: replyText = "Unknown text!"; break;
            }
            return (replyText, ReplyMarkup);
        }
    }
}
