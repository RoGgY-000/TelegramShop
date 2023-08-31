
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
        private static readonly ITelegramBotClient bot = new TelegramBotClient (AESEncoding.GetToken());
        private static Dictionary<long, Item> ItemEditCache = new();
        private static Dictionary<long, Category> CategoryEditCache = new();
        private const string badChars = "'\"*&^%$#@!{}[]`~;\\|=+<>?№";

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                DateTime start = DateTime.Now;
                Console.WriteLine("\n" + update.Type + $"  {start}");

                if (update.Type is UpdateType.Message 
                    && update.Message is not null
                    && update.Message.From is not null)
                {
                    Message message = update.Message;
                    Console.WriteLine(string.Join(", ", (message.From.FirstName, 
                                                         message.From.LastName, 
                                                         message.From.Username)));
                    string text = RemoveBadChars(message.Text ?? string.Empty);
                    Console.WriteLine(text);
                    string replyText = string.Empty;
                    var ReplyMarkup = InlineKeyboardMarkup.Empty();
                    switch (text)
                    {
                        case "/start":
                            replyText = "Здравствуйте, это магазин в телеграме.";
                            ReplyMarkup = await Kb.Menu(message.From.Id);
                            break;

                        case "/admin":
                            if (await DBMethods.IsAdmin(message.From.Id))
                            {
                                replyText = "Многофункциональная панель администратора";
                                ReplyMarkup = Kb.Admin;
                                break;
                            }
                            else goto case "/start";

                        case "/id":
                            replyText = message.From.Id.ToString();
                            break;

                        default:
                            if ( !text.StartsWith ('/') )
                            {
                                if ( await DBMethods.IsAdmin (message.From.Id) )
                                {
                                    switch ( await DBMethods.GetAdminStatus (message.From.Id) )
                                    {
                                        case AdminStatus.Clear:
                                            replyText = "Unknown text!";
                                            break;
                                        case AdminStatus.CreateCategory:
                                            if ( CategoryEditCache.ContainsKey (message.From.Id)
                                                && CategoryEditCache.TryGetValue (message.From.Id, out Category? categoryToCreate)
                                                && categoryToCreate is not null )
                                                await DBMethods.CreateCategory (text, categoryToCreate.ParentId);
                                            replyText = await DBMethods.GetCategoryByName (text) is not null
                                                ? $"Категория \"{text}\" успешно создана!"
                                                : "Не удалось создать категорию(";
                                            await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.EditCategory:
                                            if ( CategoryEditCache.ContainsKey (message.From.Id)
                                                && CategoryEditCache.TryGetValue (message.From.Id, out Category? Category)
                                                && Category is not null
                                                && DBMethods.GetCategory (Category.CategoryId) is not null )
                                            {
                                                await DBMethods.EditCategoryName (Category.CategoryId, text);
                                                if ( (await DBMethods.GetCategory (Category.CategoryId)).CategoryName == text )
                                                    replyText = "Название категории успешно изменено.";
                                                else
                                                    replyText = "failed";
                                                RemoveFromDictionaries (message.From.Id);
                                                await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                            }
                                            break;
                                        case AdminStatus.DeleteCategory:
                                            if ( CategoryEditCache.ContainsKey (message.From.Id)
                                                && CategoryEditCache.TryGetValue (message.From.Id, out Category? category) )
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
                                                    await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                                    RemoveFromDictionaries (message.From.Id);
                                                }
                                                else replyText = "Название не совпадает!";
                                            }
                                            break;
                                        case AdminStatus.CreateItemName:
                                            if ( ItemEditCache.ContainsKey (message.From.Id) )
                                            {
                                                ItemEditCache[message.From.Id].ItemName = message.Text ?? string.Empty;
                                                await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.CreateItemDesc);
                                                replyText = "Введите описание товара (введите '_', чтобы оставить пустым):";
                                            }
                                            break;
                                        case AdminStatus.CreateItemDesc:
                                            if ( ItemEditCache.ContainsKey (message.From.Id) )
                                            {
                                                ItemEditCache[message.From.Id].Description = (message.Text == "_") ? null : message.Text;
                                                await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.CreateItemPrice);
                                                replyText = "Введите цену товара:";
                                            }
                                            break;
                                        case AdminStatus.CreateItemPrice:
                                            if ( ItemEditCache.ContainsKey (message.From.Id) )
                                            {
                                                ItemEditCache[message.From.Id].ItemPriceId = int.TryParse (text, out int price) ? price : 0;
                                                await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.CreateItemImage);
                                                replyText = "Отправьте карточку товара:";
                                            }
                                            break;
                                        case AdminStatus.CreateItemImage:
                                            if ( message.Photo is null )
                                            {
                                                var stream = new MemoryStream ();
                                                //    Console.WriteLine (message.Photo.Last ().FileId);
                                                if ( ItemEditCache.ContainsKey (message.From.Id)
                                                    && ItemEditCache.TryGetValue (message.From.Id, out Item? Item)
                                                    && Item is not null )
                                                {
                                                    await DBMethods.CreateItem (
                                                        Item.CategoryId,
                                                        Item.ItemName,
                                                        Item.Description,
                                                        Item.ItemPriceId);
                                                    if ( DBMethods.GetItem (Item.ItemId) is not null )
                                                    {
                                                        replyText = $"Товар {Item.ItemName} успешно создан";
                                                        ReplyMarkup = await Kb.Back ($"edit_category {Item.CategoryId:d10}");
                                                    }
                                                }
                                            }
                                            await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                            RemoveFromDictionaries (message.From.Id);
                                            break;
                                        case AdminStatus.EditItemName:
                                            if ( ItemEditCache.ContainsKey (message.From.Id)
                                                && ItemEditCache.TryGetValue (message.From.Id, out Item? item)
                                                && item is not null
                                                && await DBMethods.GetItem (item.ItemId) is not null )
                                            { 
                                                    await DBMethods.EditItemName (item.ItemId, text);
                                                if ( (await DBMethods.GetItem (item.ItemId)).ItemName == text )
                                                    replyText = "Название товара успешно изменено.";
                                            }
                                            RemoveFromDictionaries (message.From.Id);
                                            await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.EditItemPrice:
                                            if ( ItemEditCache.ContainsKey (message.From.Id)
                                                && ItemEditCache.TryGetValue (message.From.Id, out Item? itemToEdit)
                                                && itemToEdit is not null
                                                && await DBMethods.GetItem (itemToEdit.ItemId) is not null
                                                && int.TryParse (text, out int newPrice))
                                            {
                                                await DBMethods.EditItemPrice (itemToEdit.ItemId, newPrice);
                                                if ( (await DBMethods.GetItem (itemToEdit.ItemId)).ItemPriceId == newPrice )
                                                    replyText = "Цена товара успешно изменена.";
                                            }
                                            RemoveFromDictionaries (message.From.Id);
                                            await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.EditItemDesc:
                                            if ( ItemEditCache.ContainsKey (message.From.Id)
                                                && ItemEditCache.TryGetValue (message.From.Id, out Item? ItemToEdit)
                                                && ItemToEdit is not null
                                                && await DBMethods.GetItem (ItemToEdit.ItemId) is not null )
                                            {
                                                await DBMethods.EditItemDesc (ItemToEdit.ItemId, text);
                                                if ( (await DBMethods.GetItem (ItemToEdit.ItemId)).Description == text)
                                                    replyText = "Описание товара успешно изменено.";
                                            }
                                            RemoveFromDictionaries (message.From.Id);
                                            await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                            break;
                                        case AdminStatus.DeleteItem:
                                            if ( ItemEditCache.ContainsKey (message.From.Id)
                                                && ItemEditCache.TryGetValue (message.From.Id, out Item? itemToDelete)
                                                && itemToDelete is not null)
                                            {
                                                itemToDelete = await DBMethods.GetItem (itemToDelete.ItemId);
                                                if ( itemToDelete is not null && text == itemToDelete.ItemName )
                                                {
                                                    await DBMethods.DeleteItem (itemToDelete.ItemId);
                                                    if ( !await DBMethods.ItemExists (itemToDelete.ItemId) )
                                                        replyText = $"Товар \"{itemToDelete.ItemName}\" успешно удалён";
                                                    else
                                                        replyText = $"Не удалось удалить товар \"{itemToDelete.ItemName}\"!";
                                                    await DBMethods.SetAdminStatus (message.From.Id, AdminStatus.Clear);
                                                    RemoveFromDictionaries (message.From.Id);
                                                }
                                                else replyText = "Название не совпадает!";
                                            }
                                            break;
                                        default: replyText = "Unknown text!"; break;
                                    }
                                }
                            }
                            else replyText = "Unknown command!";
                        break;
                    }
                    if (!string.IsNullOrEmpty(replyText))
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: message.Chat.Id,
                            text: replyText,
                            replyMarkup: (ReplyMarkup == null) ? new ReplyKeyboardRemove() : ReplyMarkup,
                            cancellationToken: cancellationToken,
                            parseMode: ParseMode.Html);
                        await botClient.DeleteMessageAsync(
                            chatId: message.Chat.Id,
                            messageId: message.MessageId);
                        double end = (DateTime.Now - start).TotalMilliseconds;
                        Console.WriteLine ($"Responsed in {(int) end}ms");
                        return;
                    } // send and delete message
                }
                else if (update.Type is UpdateType.CallbackQuery 
                    && update.CallbackQuery is not null
                    && update.CallbackQuery.Data is not null)
                {
                    CallbackQuery query = update.CallbackQuery;
                    Console.WriteLine(string.Join(", ", (query.From.FirstName, 
                                                         query.From.LastName, 
                                                         query.From.Username)));
                    Console.WriteLine(query.Data);
                    string ReplyText = string.Empty;
                    var ReplyMarkup = InlineKeyboardMarkup.Empty();
                    switch (query.Data)
                    {
                        case "menu":
                            ReplyText = "Здравствуйте, это магазин в телеграме.";
                            ReplyMarkup = await Kb.Menu(query.From.Id);
                            break;
                        case "catalog":
                            ReplyText = "Каталог:";
                            Category[] categories = await DBMethods.GetRootCategories();
                            if (categories == null || categories.Length == 0)
                                ReplyText = "К сожалению, пока наш каталог пуст :(";
                            ReplyMarkup = await Kb.Catalog(categories);
                            break;
                        case "cart":
                            ReplyText = $"{query.From.Username}, Ваша корзина:";
                            var carts = await DBMethods.GetUserCart(query.From.Id);
                            if (carts != null && carts.Length > 1)
                            {
                                foreach (OrderItem cart in carts)
                                {
                                    Item item = await DBMethods.GetItem(cart.ItemId);
                                    ReplyText += $"\n{item.ItemName} - {cart.Count} - {item.ItemPriceId * cart.Count}$";
                                }
                            }
                            else if (carts == null || carts.Length == 0)
                            {
                                ReplyText = "Пока что ваша корзина пуста, быстрее за покупками!";
                                ReplyMarkup = InlineKeyboardMarkup.Empty();
                            }
                            break;
                        case "admin":
                            if (await DBMethods.IsAdmin(query.From.Id) )
                            {
                                ReplyText = "Многофункциональная панель администратора";
                                ReplyMarkup = Kb.Admin;
                            }
                            break;
                        case "edit_catalog":
                            var CategoryArray = await DBMethods.GetRootCategories();
                            if ( (byte) await DBMethods.GetAdminStatus(query.From.Id) > 0) await DBMethods.SetAdminStatus(query.From.Id, AdminStatus.Clear);
                            RemoveFromDictionaries(query.From.Id);
                            if (CategoryArray == null || CategoryArray.Length < 1)
                            {
                                ReplyText = "К сожалению, пока наш каталог пуст :(";
                            }
                            else if (CategoryArray != null && CategoryArray.Length > 0)
                            {
                                ReplyText = $"Выберите категорию:\n";
                            }
                            ReplyMarkup = await Kb.EditCatalog(CategoryArray);
                            break;
                        case "orders":

                            //orders

                            break;

                        default:
                            if (query.Data[..4] == "item")
                            {
                                if (int.TryParse(query.Data[4..14], out int itemId))
                                {
                                    Byte.TryParse(query.Data.Substring(query.Data.Length - 3), out byte page);
                                    Item item = await DBMethods.GetItem(itemId);
                                    ReplyText = $"Артикул: {item.ItemId}\n\n{item.ItemName}\n\n{item.ItemPriceId}\n\n{item.Description}";
                                    ReplyMarkup = await Kb.Item(itemId, page);
                                }
                            }

                            else if (query.Data[..7] == "in_cart")
                            {
                                if (int.TryParse(query.Data[7..17], out int itemId))
                                {
                                    byte page = Byte.Parse(query.Data.Substring(query.Data.Length - 3));
                                    ReplyText = "Введите количество:";
                                    ReplyMarkup = await Kb.SelectCount(itemId, page);
                                    //await DBMethods.AddToCart(query.From.Id, itemId, )
                                }
                            }

                            else if ( query.Data[..15] == "create_category" )
                            {
                                if ( int.TryParse (query.Data[15..26], out int parentId) )
                                {
                                    RemoveFromDictionaries(query.From.Id);
                                    if ( await DBMethods.IsAdmin(query.From.Id) 
                                        && await DBMethods.GetAdminStatus (query.From.Id) == AdminStatus.Clear)
                                    {

                                        ReplyText = "Введите название категории:";
                                        ReplyMarkup = await Kb.CreateCategory ( parentId );
                                        await DBMethods.SetAdminStatus (query.From.Id, AdminStatus.CreateCategory);
                                        var category = new Category { ParentId = parentId };
                                        CategoryEditCache.Add (query.From.Id, category);
                                    }
                                    else
                                        ReplyText = "fail(";
                                }
                            }

                            else if (query.Data[..14] == "edit_category ")
                            {
                                if (int.TryParse(query.Data[14..24], out int categoryId))
                                {
                                    RemoveFromDictionaries(query.From.Id);
                                    if ( categoryId == 0 )
                                        goto case "edit_catalog";
                                    var category = await DBMethods.GetCategory(categoryId);
                                    if (category != null)
                                    {
                                        ReplyText = await DBMethods.GetStringPath (category);
                                        ReplyMarkup = await Kb.EditCategory(categoryId);
                                    }
                                    else ReplyText = "Fail(";
                                }

                            }

                            else if (query.Data[..18] == "edit_category_name")
                            {
                                if (int.TryParse(query.Data[18..29], out int categoryId))
                                {
                                    RemoveFromDictionaries(query.From.Id);
                                    ReplyText = "Введите новое название категории:";
                                    ReplyMarkup = await Kb.Back($"edit_category {categoryId:d10}");
                                    CategoryEditCache.Add(query.From.Id, await DBMethods.GetCategory(categoryId));
                                    await DBMethods.SetAdminStatus(query.From.Id, AdminStatus.EditCategory);
                                }
                            }

                            else if (query.Data[..10] == "edit_items")
                            {
                                if (int.TryParse(query.Data[10..21], out int categoryId))
                                {
                                    RemoveFromDictionaries(query.From.Id);
                                    ReplyText = await DBMethods.GetStringPath (await DBMethods.GetCategory (categoryId));
                                    Item[] items = await DBMethods.GetItemsByCategory(categoryId);
                                    int itemCount = (items == null) ? 0 : items.Length;
                                    ReplyText += $"\nКоличество товаров: {itemCount}";
                                    ReplyMarkup = await Kb.EditItemsInCategory(items, categoryId);
                                }
                                else ReplyText = "fail";
                            }

                            else if (query.Data[..11] == "create_item")
                            {
                                if (int.TryParse(query.Data[11..22], out int categoryId))
                                {
                                    RemoveFromDictionaries(query.From.Id);
                                    if (await DBMethods.IsAdmin(query.From.Id) )
                                    {
                                        ReplyText = "Введите название товара:";
                                        ReplyMarkup = await Kb.Back($"edit_items {categoryId:d10}");
                                        await DBMethods.SetAdminStatus(query.From.Id, AdminStatus.CreateItemName);
                                        var item = new Item { CategoryId = categoryId };
                                        ItemEditCache.Add(query.From.Id, item);
                                    }
                                    else ReplyText = "fail(";
                                }
                            }
                             
                            else if (query.Data[..10] == "edit_item ")
                            {
                                if (int.TryParse(query.Data[9..20], out int itemId))
                                {
                                    Item item = await DBMethods.GetItem(itemId);
                                    ReplyText = $"Название: {item.ItemName}\n" +
                                                $"Цена: {item.ItemPriceId:c}\n" +
                                                $"Описание: {item.Description?? "Пусто"}\n" +
                                                $"Категория: {await DBMethods.GetStringPath (item)}\n" +
                                                $"Артикул: {item.ItemId:d10}";
                                    ReplyMarkup = await Kb.EditItem (itemId);
                                }
                            }

                            else if ( query.Data[..14] == "edit_item_name" )
                            {
                                if ( int.TryParse (query.Data[14..25], out int itemId) )
                                {
                                    RemoveFromDictionaries (query.From.Id);
                                    Item item = await DBMethods.GetItem (itemId);
                                    await DBMethods.SetAdminStatus (query.From.Id, AdminStatus.EditItemName);
                                    ItemEditCache.Add (query.From.Id, item);
                                    ReplyText = "Введите новое название товара:";
                                    ReplyMarkup = await Kb.Back ( $"edit_item {item.ItemId:d10}");
                                }
                            }

                            else if ( query.Data[..15] == "edit_item_price" )
                            {
                                if ( int.TryParse (query.Data[15..26], out int itemId) )
                                {
                                    RemoveFromDictionaries (query.From.Id);
                                    Item item = await DBMethods.GetItem (itemId);
                                    await DBMethods.SetAdminStatus (query.From.Id, AdminStatus.EditItemPrice);
                                    ItemEditCache.Add (query.From.Id, item);
                                    ReplyText = "Введите новую цену товара:";
                                    ReplyMarkup = await Kb.Back ($"edit_item {item.ItemId:d10}");
                                }
                            }

                            else if ( query.Data[..14] == "edit_item_desc" )
                            {
                                if ( int.TryParse (query.Data[14..25], out int itemId) )
                                {
                                    RemoveFromDictionaries (query.From.Id);
                                    Item item = await DBMethods.GetItem (itemId);
                                    await DBMethods.SetAdminStatus (query.From.Id, AdminStatus.EditItemDesc);
                                    ItemEditCache.Add (query.From.Id, item);
                                    ReplyText = "Введите новое описание товара:";
                                    ReplyMarkup = await Kb.Back ($"edit_item {item.ItemId:d10}");
                                }
                            }

                            else if (query.Data[..15] == "delete_category")
                            {
                                if (int.TryParse(query.Data[15..26], out int categoryId))
                                {
                                    RemoveFromDictionaries (query.From.Id);
                                    Category category = await DBMethods.GetCategory (categoryId);
                                    if ( category != null )
                                    {
                                        CategoryEditCache.Add (query.From.Id, await DBMethods.GetCategory (categoryId));
                                        await DBMethods.SetAdminStatus (query.From.Id, AdminStatus.DeleteCategory);
                                        ReplyText = $"Введите название категории (сохраняя регистр) для удаления этой категории:";
                                        ReplyMarkup = await Kb.Back ($"edit_category {category.ParentId:d10}");
                                    }
                                }
                            }

                            else if ( query.Data[..11] == "delete_item" )
                            {
                                if ( int.TryParse (query.Data[11..22], out int itemId) )
                                {
                                    RemoveFromDictionaries (query.From.Id);
                                    Item item = await DBMethods.GetItem(itemId);
                                    if ( item is not null )
                                    {
                                        ItemEditCache.Add (query.From.Id, item);
                                        await DBMethods.SetAdminStatus (query.From.Id, AdminStatus.DeleteItem);
                                        ReplyText = $"Введите название товара (сохраняя регистр) для удаления этого товара:";
                                        ReplyMarkup = await Kb.Back ($"edit_category {item.CategoryId:d10}");
                                    }
                                }
                            }

                            else if ( query.Data[..15] == "edit_categories" )
                            {
                                if ( int.TryParse (query.Data[15..26], out int parentId) )
                                {
                                    RemoveFromDictionaries(query.From.Id);
                                    var childCategories = await DBMethods.GetChildCategories (parentId);
                                    if ( childCategories is not null )
                                    {
                                        ReplyText = $"Подкатегорий: {childCategories.Length}";
                                        ReplyMarkup = await Kb.EditCategories (parentId);
                                    }
                                }
                            }
                            break;
                    }
                    if (!string.IsNullOrEmpty(ReplyText))
                    {
                        await botClient.EditMessageTextAsync(
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
            catch (Exception e) 
            { 
                Console.WriteLine (e);
                await botClient.SendTextMessageAsync (
                            chatId: (update.Type == UpdateType.Message) ? update.Message.Chat.Id : update.CallbackQuery.From.Id,
                            text: "Что-то пошло не так, попробуйте ещё раз (",
                            replyMarkup: null,
                            cancellationToken: cancellationToken,
                            parseMode: ParseMode.Html);
                return; 
            }
        }
        // Error Handler
        private static async Task HandleErrorAsync (ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine (JsonConvert.SerializeObject (exception));
        }

        private static string RemoveBadChars(string text)
        {
            if ( text is not null )
            {
                var result = new StringBuilder (string.Empty);
                foreach ( char c in text )
                {
                    if ( !badChars.Contains (c) ) result.Append (c);
                }
                return result.ToString ();
            }
            return string.Empty;
        }

        private static void RemoveFromDictionaries (long id)
        {
            if ( CategoryEditCache.ContainsKey(id) )
                CategoryEditCache.Remove(id);
            if ( ItemEditCache.ContainsKey(id) )
                ItemEditCache.Remove(id);
        }

        private static void Main()
        {
            try
            {
                var me = bot.GetMeAsync ();
                var cts = new CancellationTokenSource ();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { },
                };
                bot.StartReceiving (
                    HandleUpdateAsync,
                    HandleErrorAsync,
                    receiverOptions,
                    cancellationToken
                );
                Console.WriteLine ("Ready");
                Console.ReadLine ();
            }
            catch ( Exception e ) { Console.WriteLine (e); }
        }
    }
}
