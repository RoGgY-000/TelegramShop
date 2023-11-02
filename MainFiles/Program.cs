
namespace TelegramShop
{
    using Telegram.Bot;
    using Telegram.Bot.Types.ReplyMarkups;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types;
    using TelegramShop.Caching;
    using TelegramShop.Routing;
    using TelegramShop.Enums;
    using TelegramShop.Keyboards;
    using TelegramShop.DataBase;
    using TelegramShop.AES;
    using Update = Telegram.Bot.Types.Update;
    using Newtonsoft.Json;
    using System.Text;

    internal class Program
    {   // Bot
        private static ITelegramBotClient bot = new TelegramBotClient (AESEncoding.GetToken ());
        private static Dictionary<long, Item> ItemCache = new ();
        private static Dictionary<long, Category> CategoryCache = new ();
        private static Dictionary<long, Store> StoreCache = new ();
        private const string badChars = "'\"*&^%$#@!{}[]`~;\\|=+<>?№";

        private static async Task HandleUpdateAsync (ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            DateTime start = DateTime.Now;
            Console.WriteLine ("\n" + update.Type + " " + start);
            long userId = Router.GetUserId (update);
            string Query = Router.GetQuery (update);
            bool hasPermission = await Cache.HasPermission (userId, Query);
            (string ReplyText, InlineKeyboardMarkup ReplyMarkup) = (string.Empty, InlineKeyboardMarkup.Empty ());
            if ( await Cache.HasPermission (userId, Query) )
                (ReplyText, ReplyMarkup) = await Router.GetResponseAsync (update);
            else
                ReplyText = "Недостаточно прав!";
            if ( update.Type is UpdateType.Message
                && update.Message is not null
                && update.Message.Text is not null
                && update.Message.From is not null )
            {
                Message message = update.Message;
                Console.WriteLine (message.From.Username);
                Console.WriteLine (message.Text);
                await botClient.SendTextMessageAsync (update.Message.Chat.Id,
                    ReplyText,
                    replyMarkup: ReplyMarkup);
            }

            else if ( update.Type is UpdateType.CallbackQuery
                && update.CallbackQuery is not null
                && update.CallbackQuery.Message is not null
                && update.CallbackQuery.Data is not null 
                && update.CallbackQuery.From is not null)
            {
                CallbackQuery query = update.CallbackQuery;
                Console.WriteLine (string.Join (", ", (query.From.FirstName,
                                                        query.From.LastName,
                                                        query.From.Username)));
                Console.WriteLine (query.Data);
                await botClient.EditMessageTextAsync (chatId: query.From.Id,
                    messageId: query.Message.MessageId,
                    text: ReplyText,
                    replyMarkup: ReplyMarkup);
            }
            double end = (DateTime.Now - start).TotalMilliseconds;
            Console.WriteLine ($"Responsed in {(int) end}ms");
            
        }

        //private static async Task HandleUpdateAsyncOld (ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        DateTime start = DateTime.Now;
        //        Console.WriteLine ("\n" + update.Type + start);

        //        if ( update.Type is UpdateType.Message
        //            && update.Message is not null
        //            && update.Message.From is not null )
        //        {
        //            Message message = update.Message;

        //            Console.WriteLine (message.From.Username);
        //            string text = RemoveBadChars (message.Text ?? string.Empty);
        //            long userId = message.From.Id;
        //            Console.WriteLine (text);
        //            string replyText = string.Empty;
        //            var ReplyMarkup = InlineKeyboardMarkup.Empty ();
        //            switch ( text )
        //            {
        //                case "/start":
        //                    replyText = "Здравствуйте, это магазин в телеграме.";
        //                    ReplyMarkup = await Kb.Menu (userId);
        //                    break;

        //                case "/id":
        //                    replyText = userId.ToString ();
        //                    break;
        //            }
        //            if ( await Db.IsAdmin (userId) )
        //            {
        //                switch ( text )
        //                {
        //                    case "/admin":
        //                        replyText = "Многофункциональная панель администратора";
        //                        ReplyMarkup = Kb.Admin;
        //                        break;

        //                    default:
        //                        if ( !text.StartsWith ('/') )
        //                        {
        //                            switch ( await Db.GetAdminStatus (userId) )
        //                            {
        //                                case AdminStatus.CreateCategory:
        //                                    if ( CategoryCache.ContainsKey (userId)
        //                                        && CategoryCache.TryGetValue (userId, out Category? categoryToCreate)
        //                                        && categoryToCreate is not null )
        //                                        await Db.CreateCategory (text, categoryToCreate.ParentId);
        //                                    replyText = await Db.GetCategoryByName (text) is not null
        //                                        ? $"Категория \"{text}\" успешно создана!"
        //                                        : "Не удалось создать категорию!";
        //                                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                    break;
        //                                case AdminStatus.EditCategory:
        //                                    if ( CategoryCache.ContainsKey (userId)
        //                                        && CategoryCache.TryGetValue (userId, out Category? Category)
        //                                        && Category is not null
        //                                        && Db.GetCategory (Category.CategoryId) is not null )
        //                                    {
        //                                        await Db.EditCategoryName (Category.CategoryId, text);
        //                                        replyText = (await Db.GetCategory (Category.CategoryId)).CategoryName == text
        //                                            ? "Название категории успешно изменено." : "failed";
        //                                        RemoveFromDictionaries (userId);
        //                                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                    }
        //                                    break;
        //                                case AdminStatus.DeleteCategory:
        //                                    if ( CategoryCache.ContainsKey (userId)
        //                                        && CategoryCache.TryGetValue (userId, out Category? category)
        //                                        && category is not null
        //                                        && text == (await Db.GetCategory (category.CategoryId)).CategoryName )
        //                                    {
        //                                        await Db.DeleteCategory (category.CategoryId);
        //                                        replyText = !await Db.CategoryExists (category.CategoryId)
        //                                            ? $"Категория \"{category.CategoryName}\" и все категории и товары в ней успешно удалены"
        //                                            : $"Не удалось удалить категорию \"{category.CategoryName}\"!";
        //                                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                        RemoveFromDictionaries (userId);
        //                                    }
        //                                    else 
        //                                        replyText = "Название не совпадает!";
        //                                    break;
        //                                case AdminStatus.DeleteItem:
        //                                    if ( ItemCache.ContainsKey (userId)
        //                                        && ItemCache.TryGetValue (userId, out Item? itemToDelete)
        //                                        && itemToDelete is not null
        //                                        && text == (await Db.GetItem (itemToDelete.ItemId)).ItemName )
        //                                    {
        //                                        await Db.DeleteItem (itemToDelete.ItemId);
        //                                        replyText = !await Db.ItemExists (itemToDelete.ItemId)
        //                                            ? $"Товар \"{itemToDelete.ItemName}\" успешно удалён"
        //                                            : $"Не удалось удалить товар \"{itemToDelete.ItemName}\"!";
        //                                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                        RemoveFromDictionaries (userId);
        //                                    }
        //                                    else 
        //                                        replyText = "Название не совпадает!";
        //                                    break;
        //                                case AdminStatus.CreateItemName:
        //                                    if ( ItemCache.ContainsKey (userId) )
        //                                    {
        //                                        ItemCache[userId].ItemName = message.Text ?? string.Empty;
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemGlobalPrice);
        //                                        replyText = "Введите стартовую (стандартную) цену товара:";
        //                                    }
        //                                    break;
        //                                case AdminStatus.CreateItemGlobalPrice:
        //                                    if ( int.TryParse (text, out int newPrice)
        //                                        && ItemCache.ContainsKey (userId)
        //                                        && ItemCache.TryGetValue (userId, out Item item)
        //                                        && item is not null )
        //                                    {
        //                                        var globalPrice = new StoreItem { ItemId = item.ItemId, Price = newPrice, StoreId = 0 };
        //                                        await Db.CreateStoreItem (globalPrice);
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemDesc);
        //                                        replyText = "Введите описание товара (введите '_', чтобы оставить пустым):";
        //                                    }
        //                                    break;
        //                                case AdminStatus.CreateItemDesc:
        //                                    if ( ItemCache.ContainsKey (userId) )
        //                                    {
        //                                        ItemCache[userId].Description = (message.Text == "_") ? null : message.Text;
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemImage);
        //                                        replyText = "Отправьте карточку товара:";
        //                                    }
        //                                    break;
        //                                case AdminStatus.CreateItemImage:
        //                                    if ( message.Photo is null )
        //                                    {
        //                                        var stream = new MemoryStream ();
        //                                        //    Console.WriteLine (message.Photo.Last ().FileId);
        //                                        if ( ItemCache.ContainsKey (userId)
        //                                            && ItemCache.TryGetValue (userId, out Item? itemToCreate)
        //                                            && itemToCreate is not null )
        //                                        {
        //                                            await Db.CreateItem (itemToCreate);
        //                                            if ( await Db.GetItem (itemToCreate.ItemId) is not null )
        //                                            {
        //                                                replyText = $"Товар {itemToCreate.ItemName} успешно создан";
        //                                                ReplyMarkup = await Kb.Back ($"edit_item {itemToCreate.ItemId:d10}");
        //                                            }
        //                                        }
        //                                    }
        //                                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                    RemoveFromDictionaries (userId);
        //                                    break;
        //                                case AdminStatus.EditItemName:
        //                                    if ( ItemCache.ContainsKey (userId)
        //                                        && ItemCache.TryGetValue (userId, out Item? item1)
        //                                        && item1 is not null
        //                                        && await Db.GetItem (item1.ItemId) is not null )
        //                                    {
        //                                        await Db.EditItemName (item1.ItemId, text);
        //                                        if ( (await Db.GetItem (item1.ItemId)).ItemName == text )
        //                                            replyText = "Название товара успешно изменено.";
        //                                    }
        //                                    RemoveFromDictionaries (userId);
        //                                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                    break;
        //                                case AdminStatus.EditItemDesc:
        //                                    if ( ItemCache.ContainsKey (userId)
        //                                        && ItemCache.TryGetValue (userId, out Item? ItemToEdit)
        //                                        && ItemToEdit is not null
        //                                        && await Db.GetItem (ItemToEdit.ItemId) is not null )
        //                                    {
        //                                        await Db.EditItemDesc (ItemToEdit.ItemId, text);
        //                                        if ( (await Db.GetItem (ItemToEdit.ItemId)).Description == text )
        //                                            replyText = "Описание товара успешно изменено.";
        //                                    }
        //                                    RemoveFromDictionaries (userId);
        //                                    await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                                    break;
        //                                case AdminStatus.CreateStoreName:
        //                                    if ( StoreCache.ContainsKey (userId) )
        //                                    {
        //                                        StoreCache[userId].StoreName = text;
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateStoreCity);
        //                                        replyText = "Введите город, в котором расположен магазин:";
        //                                    }
        //                                    break;
        //                                case AdminStatus.CreateStoreCity:
        //                                    if ( StoreCache.ContainsKey (userId) )
        //                                    {
        //                                    //    StoreCache[userId].City = text;
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateStoreAdress);
        //                                        replyText = "Введите адрес магазина:";
        //                                    }
        //                                    break;
        //                                case AdminStatus.CreateStoreAdress:
        //                                    if ( StoreCache.ContainsKey (userId)
        //                                        && StoreCache.TryGetValue (userId, out Store? storeToCreate)
        //                                        && storeToCreate is not null)
        //                                    {
        //                                    //    storeToCreate.Adress = text;
        //                                        await Db.CreateStore (storeToCreate);
        //                                        if ( await Db.StoreExists (storeToCreate.StoreId) )
        //                                            replyText = "Магазин успешно создан!";
        //                                        ReplyMarkup = await Kb.Back ($"edit_store {storeToCreate.StoreId:d10}");
        //                                    }
        //                                    break;
        //                                default: replyText = "Unknown text!"; break;
        //                            }
        //                        }
        //                        break;
        //                }
        //            }
        //            else
        //                replyText = "Unknown command!";

        //            if ( !string.IsNullOrEmpty (replyText) )
        //            {
        //                await botClient.SendTextMessageAsync (
        //                    chatId: message.Chat.Id,
        //                    text: replyText,
        //                    replyMarkup: (ReplyMarkup == null) ? new ReplyKeyboardRemove () : ReplyMarkup,
        //                    parseMode: ParseMode.Html);
        //                await botClient.DeleteMessageAsync (
        //                    chatId: message.Chat.Id,
        //                    messageId: message.MessageId);
        //                double end = (DateTime.Now - start).TotalMilliseconds;
        //                Console.WriteLine ($"Responsed in {(int) end}ms");
        //                return;
        //            } // send and delete message
        //        }
        //        else if ( update.Type is UpdateType.CallbackQuery
        //            && update.CallbackQuery is not null
        //            && update.CallbackQuery.Data is not null )
        //        {
        //            CallbackQuery query = update.CallbackQuery;
        //            long userId = query.From.Id;
        //            string data = query.Data;
        //            await botClient.AnswerCallbackQueryAsync (query.Id, "Тут может быть ваш текст!", showAlert: true);
        //            Console.WriteLine (string.Join (", ", (query.From.FirstName,
        //                                                 query.From.LastName,
        //                                                 query.From.Username)));
        //            Console.WriteLine (data);
        //            string ReplyText = string.Empty;
        //            var ReplyMarkup = InlineKeyboardMarkup.Empty ();
        //            if ( await Db.IsAdmin (userId) )
        //            {
        //                switch ( data )
        //                {
        //                    case "admin":
        //                        ReplyText = "Многофункциональная панель администратора";
        //                        ReplyMarkup = Kb.Admin;
        //                        break;
        //                    case "edit_catalog":
        //                        Category[] CategoryArray = await Db.GetRootCategories ();
        //                        await Db.SetAdminStatus (userId, AdminStatus.Clear);
        //                        RemoveFromDictionaries (userId);
        //                        if ( CategoryArray == null || CategoryArray.Length < 1 )
        //                        {
        //                            ReplyText = "К сожалению, пока наш каталог пуст :(";
        //                        }
        //                        else if ( CategoryArray != null && CategoryArray.Length > 0 )
        //                        {
        //                            ReplyText = $"Выберите категорию:\n";
        //                        }
        //                        ReplyMarkup = await Kb.EditCatalog (CategoryArray);
        //                        break;
        //                    case "orders":

        //                        //orders

        //                        break;
        //                    case "stores":
        //                        ReplyText = "Магазины:";
        //                        ReplyMarkup = await Kb.Stores ();
        //                        break;
        //                    default:
        //                        RemoveFromDictionaries (userId);

        //                        if ( data[..7] == "create" )
        //                        {
        //                            if ( data[..11] == "create_item" )
        //                            {
        //                                if ( int.TryParse (data[11..22], out int categoryId) )
        //                                {
        //                                    if ( await Db.IsAdmin (userId) )
        //                                    {
        //                                        ReplyText = "Введите название товара:";
        //                                        ReplyMarkup = await Kb.Back ($"edit_items {categoryId:d10}");
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateItemName);
        //                                        var item = new Item { CategoryId = categoryId };
        //                                        ItemCache.Add (userId, item);
        //                                    }
        //                                    else ReplyText = "fail(";
        //                                }
        //                            }

        //                            else if ( data[..15] == "create_category" )
        //                            {
        //                                if ( int.TryParse (data[15..26], out int parentId) )
        //                                {
        //                                    if ( await Db.GetAdminStatus (userId) == AdminStatus.Clear )
        //                                    {
        //                                        ReplyText = "Введите название категории:";
        //                                        ReplyMarkup = await Kb.CreateCategory (parentId);
        //                                        await Db.SetAdminStatus (userId, AdminStatus.CreateCategory);
        //                                        var category = new Category { ParentId = parentId };
        //                                        CategoryCache.Add (userId, category);
        //                                    }
        //                                    else
        //                                        ReplyText = "fail(";
        //                                }
        //                            }

        //                            else if ( data == "create_store" )
        //                            {
        //                                await Db.SetAdminStatus (userId, AdminStatus.CreateStoreName);
        //                                StoreCache.Add (userId, new Store ());
        //                                ReplyText = "Введите название магазина:";
        //                                ReplyMarkup = await Kb.Back ("admin");
        //                            }
        //                        }

        //                        else if ( data[..4] == "edit" )
        //                        {
        //                            if ( data[..10] == "edit_item " )
        //                            {
        //                                if ( int.TryParse (data[9..20], out int itemId) )
        //                                {
        //                                    Item item = await Db.GetItem (itemId);
        //                                    ReplyText = $"Название: {item.ItemName}\n" +
        //                                                $"Цена:\n" +
        //                                                $"Описание: {item.Description ?? "Пусто"}\n" +
        //                                                $"Категория: {await Db.GetStringPath (item)}\n" +
        //                                                $"Артикул: {item.ItemId:d10}";
        //                                    ReplyMarkup = await Kb.EditItem (itemId);
        //                                }
        //                            }

        //                            else if ( data[..14] == "edit_item_name" )
        //                            {
        //                                if ( int.TryParse (data[14..25], out int itemId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    Item item = await Db.GetItem (itemId);
        //                                    await Db.SetAdminStatus (userId, AdminStatus.EditItemName);
        //                                    ItemCache.Add (userId, item);
        //                                    ReplyText = "Введите новое название товара:";
        //                                    ReplyMarkup = await Kb.Back ($"edit_item {item.ItemId:d10}");
        //                                }
        //                            }

        //                            else if ( data[..15] == "edit_item_price" )
        //                            {
        //                                if ( int.TryParse (data[15..26], out int itemId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);

        //                                    ReplyMarkup = await Kb.Back ($"edit_item {itemId:d10}");
        //                                }
        //                            }

        //                            else if ( data[..14] == "edit_item_desc" )
        //                            {
        //                                if ( int.TryParse (data[14..25], out int itemId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    Item item = await Db.GetItem (itemId);
        //                                    await Db.SetAdminStatus (userId, AdminStatus.EditItemDesc);
        //                                    ItemCache.Add (userId, item);
        //                                    ReplyText = "Введите новое описание товара:";
        //                                    ReplyMarkup = await Kb.Back ($"edit_item {item.ItemId:d10}");
        //                                }
        //                            }

        //                            else if ( data[..14] == "edit_category " )
        //                            {
        //                                if ( int.TryParse (data[14..24], out int categoryId) )
        //                                {
        //                                    if ( categoryId == 0 )
        //                                        goto case "edit_catalog";
        //                                    var category = await Db.GetCategory (categoryId);
        //                                    if ( category != null )
        //                                    {
        //                                        ReplyText = await Db.GetStringPath (category);
        //                                        ReplyMarkup = await Kb.EditCategory (categoryId);
        //                                    }
        //                                    else ReplyText = "Fail(";
        //                                }

        //                            }

        //                            else if ( data[..18] == "edit_category_name" )
        //                            {
        //                                if ( int.TryParse (data[18..29], out int categoryId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    ReplyText = "Введите новое название категории:";
        //                                    ReplyMarkup = await Kb.Back ($"edit_category {categoryId:d10}");
        //                                    CategoryCache.Add (userId, await Db.GetCategory (categoryId));
        //                                    await Db.SetAdminStatus (userId, AdminStatus.EditCategory);
        //                                }
        //                            }

        //                            else if ( data[..10] == "edit_items" )
        //                            {
        //                                if ( int.TryParse (data[10..21], out int categoryId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    ReplyText = await Db.GetStringPath (await Db.GetCategory (categoryId));
        //                                    Item[] items = await Db.GetItemsByCategory (categoryId);
        //                                    int itemCount = (items == null) ? 0 : items.Length;
        //                                    ReplyText += $"\nКоличество товаров: {itemCount}";
        //                                    ReplyMarkup = await Kb.EditItemsInCategory (items, categoryId);
        //                                }
        //                                else ReplyText = "fail";
        //                            }

        //                            else if ( data[..15] == "edit_categories" )
        //                            {
        //                                if ( int.TryParse (data[15..26], out int parentId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    var childCategories = await Db.GetChildCategories (parentId);
        //                                    if ( childCategories is not null )
        //                                    {
        //                                        ReplyText = $"Подкатегорий: {childCategories.Length}";
        //                                        ReplyMarkup = await Kb.EditCategories (parentId);
        //                                    }
        //                                }
        //                            }

        //                            else if ( data[..9] == "edit_store" )
        //                            {
        //                                if ( int.TryParse (data[9..20], out int storeId) )
        //                                {
        //                                    Store store = await Db.GetStore (storeId);

        //                                }
        //                            }
        //                        }

        //                        else if ( data[..6] == "delete" )
        //                        {
        //                            if ( data[..11] == "delete_item" )
        //                            {
        //                                if ( int.TryParse (data[11..22], out int itemId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    Item item = await Db.GetItem (itemId);
        //                                    if ( item is not null )
        //                                    {
        //                                        ItemCache.Add (userId, item);
        //                                        await Db.SetAdminStatus (userId, AdminStatus.DeleteItem);
        //                                        ReplyText = $"Введите название товара (сохраняя регистр) для удаления этого товара:";
        //                                        ReplyMarkup = await Kb.Back ($"edit_category {item.CategoryId:d10}");
        //                                    }
        //                                }
        //                            }

        //                            else if ( data[..15] == "delete_category" )
        //                            {
        //                                if ( int.TryParse (data[15..26], out int categoryId) )
        //                                {
        //                                    RemoveFromDictionaries (userId);
        //                                    Category category = await Db.GetCategory (categoryId);
        //                                    if ( category != null )
        //                                    {
        //                                        CategoryCache.Add (userId, await Db.GetCategory (categoryId));
        //                                        await Db.SetAdminStatus (userId, AdminStatus.DeleteCategory);
        //                                        ReplyText = $"Введите название категории (сохраняя регистр) для удаления этой категории:";
        //                                        ReplyMarkup = await Kb.Back ($"edit_category {category.ParentId:d10}");
        //                                    }
        //                                }
        //                            }
        //                        }

        //                        else ReplyText = "fail";
        //                        break;
        //                }
        //            }
        //            switch ( data )
        //            {
        //                case "menu":
        //                    ReplyText = "Здравствуйте, это магазин в телеграме.";
        //                    ReplyMarkup = await Kb.Menu (userId);
        //                    break;
        //                case "catalog":
        //                    ReplyText = "Каталог:";
        //                    Category[] categories = await Db.GetRootCategories ();
        //                    if ( categories == null || categories.Length == 0 )
        //                        ReplyText = "К сожалению, пока наш каталог пуст :(";
        //                    ReplyMarkup = await Kb.Catalog (categories);
        //                    break;
        //                case "cart":
        //                    ReplyText = $"{query.From.Username}, Ваша корзина:";
        //                    var carts = await Db.GetUserCart (userId);
        //                    if ( carts != null && carts.Length > 1 )
        //                    {
        //                    }
        //                    else if ( carts == null || carts.Length == 0 )
        //                    {
        //                        ReplyText = "Пока что ваша корзина пуста, быстрее за покупками!";
        //                        ReplyMarkup = InlineKeyboardMarkup.Empty ();
        //                    }
        //                    break;

        //                default:
        //                    if ( data[..4] == "item" )
        //                    {
        //                        if ( int.TryParse (data[4..14], out int itemId) )
        //                        {
        //                            byte.TryParse (data.Substring (data.Length - 3), out byte page);
        //                            Item item = await Db.GetItem (itemId);
        //                            ReplyText = $"Артикул: {item.ItemId}\n\n{item.ItemName}\n\n\n\n{item.Description}";
        //                            ReplyMarkup = await Kb.Item (itemId, page);
        //                        }
        //                    }

        //                    else if ( data[..7] == "in_cart" )
        //                    {
        //                        if ( int.TryParse (data[7..17], out int itemId) )
        //                        {
        //                            byte page = byte.Parse (data.Substring (data.Length - 3));
        //                            ReplyText = "Введите количество:";
        //                            ReplyMarkup = await Kb.SelectCount (itemId, page);
        //                            //await Db.AddToCart(userId, itemId, )
        //                        }
        //                    }

        //                    break;
        //            }
        //            if ( !string.IsNullOrEmpty (ReplyText) )
        //            {
        //                await botClient.EditMessageTextAsync (
        //                    chatId: query.Message.Chat.Id,
        //                    replyMarkup: ReplyMarkup,
        //                    text: ReplyText,
        //                    parseMode: ParseMode.Html,
        //                    messageId: query.Message.MessageId);
        //                double end = (DateTime.Now - start).TotalMilliseconds;
        //                Console.WriteLine ($"Responsed in {(int) end}ms");
        //            } //edit message
        //        }
        //    }
        //    catch ( Exception e )
        //    {
        //        Console.WriteLine (e);
        //        await botClient.SendTextMessageAsync (
        //                    chatId: (update.Type == UpdateType.Message) ? update.Message.Chat.Id : update.CallbackQuery.From.Id,
        //                    text: "Что-то пошло не так, попробуйте ещё раз (",
        //                    replyMarkup: null,
        //                    parseMode: ParseMode.Html);
        //    }
        //}
        //// Error Handler
        private static async Task HandleErrorAsync (ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine (JsonConvert.SerializeObject (exception));
        }
        private static void Main ()
        {
            try
            {
                AESEncoding.Init ();
                Db.Init ();
                Router.Init ();
                bot.TestApiAsync ();    
                bot.StartReceiving (
                    HandleUpdateAsync,
                    HandleErrorAsync
                );
                Console.WriteLine ("Ready");
                Console.ReadLine ();
            }
            catch ( Exception e ) 
            { Console.WriteLine (e); }
        }
    }
}
