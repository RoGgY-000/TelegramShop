
namespace TelegramShop
{
    using Telegram.Bot;
    using Telegram.Bot.Polling;
    using Telegram.Bot.Types.ReplyMarkups;
    using TelegramShop.Keyboards;
    using TelegramShop.DataBase;
    using TelegramShop.AES;
    using Update = Telegram.Bot.Types.Update;
    using Telegram.Bot.Types.Enums;
    using Newtonsoft.Json;
    using System.Resources;
    using System.Text;
    using Telegram.Bot.Types;

    internal class Program
    {   // Bot
        private static ITelegramBotClient bot = new TelegramBotClient (AESEncoding.GetToken ());
        private static Dictionary<long, Item> ItemCache = new ();
        private static Dictionary<long, Category> CategoryCache = new ();
        private static Dictionary<long, Price> PriceCache = new ();
        private const string badChars = "'\"*&^%$#@!{}[]`~;\\|=+<>?№";

        private static async Task HandleUpdateAsync (ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                DateTime start = DateTime.Now;
                Console.WriteLine ("\n" + update.Type + $"  {start}");

                if ( update.Type is UpdateType.Message
                    && update.Message is not null
                    && update.Message.From is not null )
                {
                    Message message = update.Message;

                    Console.WriteLine (string.Join (", ", (message.From.FirstName,
                                                         message.From.LastName,
                                                         message.From.Username)));
                    string text = RemoveBadChars (message.Text ?? string.Empty);
                    long userId = message.From.Id;
                    Console.WriteLine (text);
                    string replyText = string.Empty;
                    var ReplyMarkup = InlineKeyboardMarkup.Empty ();
                    switch ( text )
                    {
                        case "/start":
                            replyText = "Здравствуйте, это магазин в телеграме.";
                            ReplyMarkup = await Kb.Menu (userId);
                            break;

                        case "/admin":
                            if ( await DBMethods.IsAdmin (userId) )
                            {
                                replyText = "Многофункциональная панель администратора";
                                ReplyMarkup = Kb.Admin;
                                break;
                            }
                            else goto case "/start";

                        case "/id":
                            replyText = userId.ToString ();
                            break;

                        default:
                            if ( !text.StartsWith ('/') )
                            {
                                if ( await DBMethods.IsAdmin (userId) )
                                {
                                    switch ( await DBMethods.GetAdminStatus (userId) )
                                    {
                                        case AdminStatus.Clear:
                                            replyText = "Unknown text!";
                                            break;
                                        case AdminStatus.CreateCategory:
                                            if ( CategoryCache.ContainsKey (userId)
                                                && CategoryCache.TryGetValue (userId, out Category? categoryToCreate)
                                                && categoryToCreate is not null )
                                                await DBMethods.CreateCategory (text, categoryToCreate.ParentId);
                                            replyText = await DBMethods.GetCategoryByName (text) is not null
                                                ? $"Категория \"{text}\" успешно создана!"
                                                : "Не удалось создать категорию!";
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.EditCategory:
                                            if ( CategoryCache.ContainsKey (userId)
                                                && CategoryCache.TryGetValue (userId, out Category? Category)
                                                && Category is not null
                                                && DBMethods.GetCategory (Category.CategoryId) is not null )
                                            {
                                                await DBMethods.EditCategoryName (Category.CategoryId, text);
                                                replyText = (await DBMethods.GetCategory (Category.CategoryId)).CategoryName == text
                                                    ? "Название категории успешно изменено." : "failed";
                                                RemoveFromDictionaries (userId);
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                            }
                                            break;
                                        case AdminStatus.DeleteCategory:
                                            if ( CategoryCache.ContainsKey (userId)
                                                && CategoryCache.TryGetValue (userId, out Category? category) )
                                            {
                                                Category CategoryToDelete = await DBMethods.GetCategory (category.CategoryId);
                                                if ( CategoryToDelete is not null && text == category.CategoryName )
                                                {
                                                    await DBMethods.DeleteCategory (category.CategoryId);
                                                    if ( !await DBMethods.CategoryExists (category.CategoryId)
                                                        && (await DBMethods.GetItemsByCategory (category.CategoryId)).Length == 0 )
                                                        replyText = $"Категория \"{CategoryToDelete.CategoryName}\" и все товары в ней успешно удалены";
                                                    else
                                                        replyText = $"Не удалось удалить категорию \"{CategoryToDelete.CategoryName}\"!";
                                                    await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                                    RemoveFromDictionaries (userId);
                                                }
                                                else replyText = "Название не совпадает!";
                                            }
                                            break;
                                        case AdminStatus.DeleteItem:
                                            if ( ItemCache.ContainsKey (userId)
                                                && ItemCache.TryGetValue (userId, out Item? itemToDelete)
                                                && itemToDelete is not null
                                                && await DBMethods.GetItem (itemToDelete.ItemId) is not null
                                                && text == itemToDelete.ItemName )
                                            {
                                                await DBMethods.DeleteItem (itemToDelete.ItemId);
                                                replyText = !await DBMethods.ItemExists (itemToDelete.ItemId)
                                                    ? $"Товар \"{itemToDelete.ItemName}\" успешно удалён"
                                                    : $"Не удалось удалить товар \"{itemToDelete.ItemName}\"!";
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                                RemoveFromDictionaries (userId);
                                            }
                                            else replyText = "Название не совпадает!";
                                            break;
                                        case AdminStatus.CreateItemName:
                                            if ( ItemCache.ContainsKey (userId) )
                                            {
                                                ItemCache[userId].ItemName = message.Text ?? string.Empty;
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.CreateItemGlobalPrice);
                                                replyText = "Введите стартовую (стандартную) цену товара:";
                                            }
                                            break;
                                        case AdminStatus.CreateItemGlobalPrice:
                                            if ( int.TryParse (text, out int newPrice)
                                                && ItemCache.ContainsKey (userId)
                                                && ItemCache.TryGetValue (userId, out Item item)
                                                && item is not null)
                                            {
                                                var globalPrice = new Price { ItemId = item.ItemId, PriceValue = newPrice, StoreId = 0 };
                                                await DBPrices.CreatePrice (globalPrice);
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.CreateItemDesc);
                                                replyText = "Введите описание товара (введите '_', чтобы оставить пустым):";
                                            }
                                            break;
                                        case AdminStatus.CreateItemDesc:
                                            if ( ItemCache.ContainsKey (userId) )
                                            {
                                                ItemCache[userId].Description = (message.Text == "_") ? null : message.Text;
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.CreateItemImage);
                                                replyText = "Отправьте карточку товара:";
                                            }
                                            break;
                                        case AdminStatus.CreateItemImage:
                                            if ( message.Photo is null )
                                            {
                                                var stream = new MemoryStream ();
                                                //    Console.WriteLine (message.Photo.Last ().FileId);
                                                if ( ItemCache.ContainsKey (userId)
                                                    && ItemCache.TryGetValue (userId, out Item? itemToCreate)
                                                    && itemToCreate is not null )
                                                {
                                                    await DBItems.CreateItem (itemToCreate);
                                                    if ( await DBItems.GetItem (itemToCreate.ItemId) is not null )
                                                    {
                                                        replyText = $"Товар {itemToCreate.ItemName} успешно создан";
                                                        ReplyMarkup = await Kb.Back ($"edit_item {itemToCreate.ItemId:d10}");
                                                    }
                                                }
                                            }
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                            RemoveFromDictionaries (userId);
                                            break;
                                        case AdminStatus.EditItemName:
                                            if ( ItemCache.ContainsKey (userId)
                                                && ItemCache.TryGetValue (userId, out Item? item)
                                                && item is not null
                                                && await DBMethods.GetItem (item.ItemId) is not null )
                                            {
                                                await DBMethods.EditItemName (item.ItemId, text);
                                                if ( (await DBMethods.GetItem (item.ItemId)).ItemName == text )
                                                    replyText = "Название товара успешно изменено.";
                                            }
                                            RemoveFromDictionaries (userId);
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.EditItemDesc:
                                            if ( ItemCache.ContainsKey (userId)
                                                && ItemCache.TryGetValue (userId, out Item? ItemToEdit)
                                                && ItemToEdit is not null
                                                && await DBMethods.GetItem (ItemToEdit.ItemId) is not null )
                                            {
                                                await DBMethods.EditItemDesc (ItemToEdit.ItemId, text);
                                                if ( (await DBMethods.GetItem (ItemToEdit.ItemId)).Description == text )
                                                    replyText = "Описание товара успешно изменено.";
                                            }
                                            RemoveFromDictionaries (userId);
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.CreateStore:
                                            if ( await DBMethods.GetAdminStatus (userId) == AdminStatus.CreateStore
                                                && !string.IsNullOrEmpty (text) )
                                            {
                                                await DBShops.CreateStore (new Store { StoreName = text });
                                            }
                                            break;
                                        default: replyText = "Unknown text!"; break;
                                    }
                                }
                            }
                            else 
                                replyText = "Unknown command!";
                            break;
                    }
                    if ( !string.IsNullOrEmpty (replyText) )
                    {
                        await botClient.SendTextMessageAsync (
                            chatId: message.Chat.Id,
                            text: replyText,
                            replyMarkup: (ReplyMarkup == null) ? new ReplyKeyboardRemove () : ReplyMarkup,
                            cancellationToken: cancellationToken,
                            parseMode: ParseMode.Html);
                        await botClient.DeleteMessageAsync (
                            chatId: message.Chat.Id,
                            messageId: message.MessageId);
                        double end = (DateTime.Now - start).TotalMilliseconds;
                        Console.WriteLine ($"Responsed in {(int) end}ms");
                        return;
                    } // send and delete message
                }
                else if ( update.Type is UpdateType.CallbackQuery
                    && update.CallbackQuery is not null
                    && update.CallbackQuery.Data is not null )
                {
                    CallbackQuery query = update.CallbackQuery;
                    long userId = query.From.Id;
                    string data = query.Data;
                    await botClient.AnswerCallbackQueryAsync (query.Id, "Тут может быть ваш текст!", showAlert: true);
                    Console.WriteLine (string.Join (", ", (query.From.FirstName,
                                                         query.From.LastName,
                                                         query.From.Username)));
                    Console.WriteLine (data);
                    string ReplyText = string.Empty;
                    var ReplyMarkup = InlineKeyboardMarkup.Empty ();
                    if ( !await DBMethods.IsAdmin (userId) )
                    {
                        switch ( data )
                        {
                            case "menu":
                                ReplyText = "Здравствуйте, это магазин в телеграме.";
                                ReplyMarkup = await Kb.Menu (userId);
                                break;
                            case "catalog":
                                ReplyText = "Каталог:";
                                Category[] categories = await DBMethods.GetRootCategories ();
                                if ( categories == null || categories.Length == 0 )
                                    ReplyText = "К сожалению, пока наш каталог пуст :(";
                                ReplyMarkup = await Kb.Catalog (categories);
                                break;
                            case "cart":
                                ReplyText = $"{query.From.Username}, Ваша корзина:";
                                var carts = await DBMethods.GetUserCart (userId);
                                if ( carts != null && carts.Length > 1 )
                                {
                                }
                                else if ( carts == null || carts.Length == 0 )
                                {
                                    ReplyText = "Пока что ваша корзина пуста, быстрее за покупками!";
                                    ReplyMarkup = InlineKeyboardMarkup.Empty ();
                                }
                                break;

                            default:
                                if ( data[..4] == "item" )
                                {
                                    if ( int.TryParse (data[4..14], out int itemId) )
                                    {
                                        Byte.TryParse (data.Substring (data.Length - 3), out byte page);
                                        Item item = await DBMethods.GetItem (itemId);
                                        ReplyText = $"Артикул: {item.ItemId}\n\n{item.ItemName}\n\n\n\n{item.Description}";
                                        ReplyMarkup = await Kb.Item (itemId, page);
                                    }
                                }

                                else if ( data[..7] == "in_cart" )
                                {
                                    if ( int.TryParse (data[7..17], out int itemId) )
                                    {
                                        byte page = Byte.Parse (data.Substring (data.Length - 3));
                                        ReplyText = "Введите количество:";
                                        ReplyMarkup = await Kb.SelectCount (itemId, page);
                                        //await DBMethods.AddToCart(userId, itemId, )
                                    }
                                }

                                break;
                        }
                    }
                    else
                    {
                        switch ( data )
                        {
                            case "admin":
                                    ReplyText = "Многофункциональная панель администратора";
                                    ReplyMarkup = Kb.Admin;
                                break;
                            case "edit_catalog":
                                var CategoryArray = await DBMethods.GetRootCategories ();
                                if ( (byte) await DBMethods.GetAdminStatus (userId) > 0 ) await DBMethods.SetAdminStatus (userId, AdminStatus.Clear);
                                RemoveFromDictionaries (userId);
                                if ( CategoryArray == null || CategoryArray.Length < 1 )
                                {
                                    ReplyText = "К сожалению, пока наш каталог пуст :(";
                                }
                                else if ( CategoryArray != null && CategoryArray.Length > 0 )
                                {
                                    ReplyText = $"Выберите категорию:\n";
                                }
                                ReplyMarkup = await Kb.EditCatalog (CategoryArray);
                                break;
                            case "orders":

                                //orders

                                break;
                            case "Stores":
                                ReplyText = "Магазины:";
                                ReplyMarkup = await Kb.Stores ();
                                break;
                            default:
                                RemoveFromDictionaries (userId);

                                if ( data[..7] == "create" )
                                {
                                    if ( data[..11] == "create_item" )
                                    {
                                        if ( int.TryParse (data[11..22], out int categoryId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            if ( await DBMethods.IsAdmin (userId) )
                                            {
                                                ReplyText = "Введите название товара:";
                                                ReplyMarkup = await Kb.Back ($"edit_items {categoryId:d10}");
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.CreateItemName);
                                                var item = new Item { CategoryId = categoryId };
                                                ItemCache.Add (userId, item);
                                            }
                                            else ReplyText = "fail(";
                                        }
                                    }

                                    else if ( data[..15] == "create_category" )
                                    {
                                        if ( int.TryParse (data[15..26], out int parentId) )
                                        {
                                            if ( await DBMethods.GetAdminStatus (userId) == AdminStatus.Clear )
                                            {
                                                ReplyText = "Введите название категории:";
                                                ReplyMarkup = await Kb.CreateCategory (parentId);
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.CreateCategory);
                                                var category = new Category { ParentId = parentId };
                                                CategoryCache.Add (userId, category);
                                            }
                                            else
                                                ReplyText = "fail(";
                                        }
                                    }

                                    else if ( data == "create_shop" )
                                    {
                                        await DBMethods.SetAdminStatus (userId, AdminStatus.CreateShop);
                                        ReplyText = "Введите название магазина:";
                                        ReplyMarkup = await Kb.Back ("admin");
                                    }

                                }

                                else if ( data[..4] == "edit" )
                                {
                                    if ( data[..10] == "edit_item " )
                                    {
                                        if ( int.TryParse (data[9..20], out int itemId) )
                                        {
                                            Item item = await DBMethods.GetItem (itemId);
                                            ReplyText = $"Название: {item.ItemName}\n" +
                                                        $"Цена:\n" +
                                                        $"Описание: {item.Description ?? "Пусто"}\n" +
                                                        $"Категория: {await DBMethods.GetStringPath (item)}\n" +
                                                        $"Артикул: {item.ItemId:d10}";
                                            ReplyMarkup = await Kb.EditItem (itemId);
                                        }
                                    }

                                    else if ( data[..14] == "edit_item_name" )
                                    {
                                        if ( int.TryParse (data[14..25], out int itemId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            Item item = await DBMethods.GetItem (itemId);
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.EditItemName);
                                            ItemCache.Add (userId, item);
                                            ReplyText = "Введите новое название товара:";
                                            ReplyMarkup = await Kb.Back ($"edit_item {item.ItemId:d10}");
                                        }
                                    }

                                    else if ( data[..15] == "edit_item_price" )
                                    {
                                        if ( int.TryParse (data[15..26], out int itemId) )
                                        {
                                            RemoveFromDictionaries (userId);

                                            ReplyMarkup = await Kb.Back ($"edit_item {itemId:d10}");
                                        }
                                    }

                                    else if ( data[..14] == "edit_item_desc" )
                                    {
                                        if ( int.TryParse (data[14..25], out int itemId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            Item item = await DBMethods.GetItem (itemId);
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.EditItemDesc);
                                            ItemCache.Add (userId, item);
                                            ReplyText = "Введите новое описание товара:";
                                            ReplyMarkup = await Kb.Back ($"edit_item {item.ItemId:d10}");
                                        }
                                    }

                                    else if ( data[..14] == "edit_category " )
                                    {
                                        if ( int.TryParse (data[14..24], out int categoryId) )
                                        {
                                            if ( categoryId == 0 )
                                                goto case "edit_catalog";
                                            var category = await DBMethods.GetCategory (categoryId);
                                            if ( category != null )
                                            {
                                                ReplyText = await DBMethods.GetStringPath (category);
                                                ReplyMarkup = await Kb.EditCategory (categoryId);
                                            }
                                            else ReplyText = "Fail(";
                                        }

                                    }

                                    else if ( data[..18] == "edit_category_name" )
                                    {
                                        if ( int.TryParse (data[18..29], out int categoryId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            ReplyText = "Введите новое название категории:";
                                            ReplyMarkup = await Kb.Back ($"edit_category {categoryId:d10}");
                                            CategoryCache.Add (userId, await DBMethods.GetCategory (categoryId));
                                            await DBMethods.SetAdminStatus (userId, AdminStatus.EditCategory);
                                        }
                                    }

                                    else if ( data[..10] == "edit_items" )
                                    {
                                        if ( int.TryParse (data[10..21], out int categoryId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            ReplyText = await DBMethods.GetStringPath (await DBMethods.GetCategory (categoryId));
                                            Item[] items = await DBMethods.GetItemsByCategory (categoryId);
                                            int itemCount = (items == null) ? 0 : items.Length;
                                            ReplyText += $"\nКоличество товаров: {itemCount}";
                                            ReplyMarkup = await Kb.EditItemsInCategory (items, categoryId);
                                        }
                                        else ReplyText = "fail";
                                    }

                                    else if ( data[..15] == "edit_categories" )
                                    {
                                        if ( int.TryParse (data[15..26], out int parentId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            var childCategories = await DBMethods.GetChildCategories (parentId);
                                            if ( childCategories is not null )
                                            {
                                                ReplyText = $"Подкатегорий: {childCategories.Length}";
                                                ReplyMarkup = await Kb.EditCategories (parentId);
                                            }
                                        }
                                    }

                                    else if ( data[..9] == "edit_shop" )
                                    {
                                        if ( int.TryParse (data[9..20], out int storeId) )
                                        {
                                            Store store = await DBStores.GetStore (storeId);

                                        }
                                    }
                                }

                                else if ( data[..6] == "delete" )
                                {
                                    if ( data[..11] == "delete_item" )
                                    {
                                        if ( int.TryParse (data[11..22], out int itemId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            Item item = await DBMethods.GetItem (itemId);
                                            if ( item is not null )
                                            {
                                                ItemCache.Add (userId, item);
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.DeleteItem);
                                                ReplyText = $"Введите название товара (сохраняя регистр) для удаления этого товара:";
                                                ReplyMarkup = await Kb.Back ($"edit_category {item.CategoryId:d10}");
                                            }
                                        }
                                    }
                                    else if ( data[..15] == "delete_category" )
                                    {
                                        if ( int.TryParse (data[15..26], out int categoryId) )
                                        {
                                            RemoveFromDictionaries (userId);
                                            Category category = await DBMethods.GetCategory (categoryId);
                                            if ( category != null )
                                            {
                                                CategoryCache.Add (userId, await DBMethods.GetCategory (categoryId));
                                                await DBMethods.SetAdminStatus (userId, AdminStatus.DeleteCategory);
                                                ReplyText = $"Введите название категории (сохраняя регистр) для удаления этой категории:";
                                                ReplyMarkup = await Kb.Back ($"edit_category {category.ParentId:d10}");
                                            }
                                        }
                                    }
                                }

                                else ReplyText = "fail";
                                break;
                        }
                    }
                    if ( !string.IsNullOrEmpty (ReplyText) )
                    {
                        await botClient.EditMessageTextAsync (
                            chatId: query.Message.Chat.Id,
                            replyMarkup: ReplyMarkup,
                            text: ReplyText,
                            parseMode: ParseMode.Html,
                            messageId: query.Message.MessageId);
                        double end = (DateTime.Now - start).TotalMilliseconds;
                        Console.WriteLine ($"Responsed in {(int) end}ms");
                    } //edit message
                }
            }
            catch ( Exception e )
            {
                Console.WriteLine (e);
                await botClient.SendTextMessageAsync (
                            chatId: (update.Type == UpdateType.Message) ? update.Message.Chat.Id : update.CallbackQuery.From.Id,
                            text: "Что-то пошло не так, попробуйте ещё раз (",
                            replyMarkup: null,
                            cancellationToken: cancellationToken,
                            parseMode: ParseMode.Html);
            }
        }
        // Error Handler
        private static async Task HandleErrorAsync (ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine (JsonConvert.SerializeObject (exception));
        }

        private static string RemoveBadChars (string text)
        {
            if ( text is not null )
            {
                var result = new StringBuilder (string.Empty);
                foreach ( char c in text )
                {
                    if ( !badChars.Contains (c) ) 
                        result.Append (c);
                }
                return result.ToString ();
            }
            return string.Empty;
        }

        private static void RemoveFromDictionaries (long id)
        {
            if ( CategoryCache.ContainsKey (id) )
                CategoryCache.Remove (id);
            if ( ItemCache.ContainsKey (id) )
                ItemCache.Remove (id);
            if ( PriceCache.ContainsKey (id) )
                PriceCache.Remove (id);
        }

        private static void Main ()
        {
            try
            {
                bot.TestApiAsync ();
                bot.StartReceiving (
                    HandleUpdateAsync,
                    HandleErrorAsync
                );
                Console.WriteLine ("Ready");
                Console.ReadLine ();
            }
            catch ( Exception e ) { Console.WriteLine (e); }
        }
    }
}
