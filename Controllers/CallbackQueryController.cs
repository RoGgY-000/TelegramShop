namespace TelegramShop.Controllers
{
    using Telegram.Bot.Types.ReplyMarkups;
    using Telegram.Bot.Types;
    using TelegramShop.Enums;
    using TelegramShop.Caching;
    using TelegramShop.DataBase;
    using TelegramShop.Routing;
    using TelegramShop.Keyboards;
    using TelegramShop.Attributes;

    internal class CallbackQueryController
    {
        [Route ("edit_catalog")]
        public static async Task<(string, InlineKeyboardMarkup)> EditCatalog (Update update, int page = 0)
        {
            string ReplyText = string.Empty;
            long userId = Router.GetUserId (update);
            Category[] CategoryArray = await Db.GetRootCategories ();
            await Db.SetAdminStatus (userId, AdminStatus.Clear);
            Db.RemoveUser (userId);
            if ( CategoryArray == null || CategoryArray.Length < 1 )
                ReplyText = "К сожалению, пока наш каталог пуст :(";
            else if ( CategoryArray is not null && CategoryArray.Length > 0 )
                ReplyText = $"Выберите категорию:\n";
            return (ReplyText, await Kb.EditCatalog (CategoryArray));
        }

        [Route ("create_category")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateCategory (Update update, int parentId = 0)
        {
            long userId = Router.GetUserId (update);
            string ReplyText;
            if ( await Db.GetAdminStatus (userId) is AdminStatus.Clear )
            {
                ReplyText = "Введите название категории:";
                await Db.SetAdminStatus (userId, AdminStatus.CreateCategory);
                var category = new Category { ParentId = parentId };
                Db.AddToCache (userId, category);
            }
            else
                ReplyText = "fail(";
            return (ReplyText, await Kb.CreateCategory (parentId));
        }

        [Route ("edit_category")]
        public static async Task<(string, InlineKeyboardMarkup)> EditCategory (Update update, int id)
        {
            if ( id == 0 )
                return await EditCatalog (update);
            string ReplyText = string.Empty;
            var category = await Db.GetCategory (id);
            await Db.SetAdminStatus (update.CallbackQuery.From.Id, AdminStatus.Clear);
            Db.RemoveUser (update.CallbackQuery.From.Id);
            if ( category != null )
                ReplyText = await Db.GetStringPath (category);
            return (ReplyText, await Kb.EditCategory (id));
        }

        [Route ("edit_categories")]
        public static async Task<(string, InlineKeyboardMarkup)> EditCategories (Update update, int id)
        {
            string ReplyText = string.Empty;
            var childCategories = await Db.GetChildCategories (id);
            if ( childCategories is not null )
                ReplyText = $"Подкатегорий: {childCategories.Length}";
            return (ReplyText, await Kb.EditCategories (id));
        }

        [Route ("delete_category")]
        public static async Task<(string, InlineKeyboardMarkup)> DeleteCategory (Update update, int id)
        {
            string ReplyText = string.Empty;
            Category category = await Db.GetCategory (id);
            long userId = Router.GetUserId (update);
            if ( category != null )
            {
                Db.AddToCache (userId, category);
                await Db.SetAdminStatus (userId, AdminStatus.DeleteCategory);
                ReplyText = $"Введите название категории (сохраняя регистр) для удаления этой категории:";

            }
            return (ReplyText, await Kb.Back ($"edit_category?id={category.ParentId:d10}"));
        }

        [Route ("edit_category_name")]
        public static async Task<(string, InlineKeyboardMarkup)> EditCategoryName (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            string ReplyText = "Введите новое название категории:";
            Db.AddToCache (userId, await Db.GetCategory (id));
            await Db.SetAdminStatus (userId, AdminStatus.EditCategory);
            return (ReplyText, await Kb.Back ($"edit_category?id={id:d10}"));
        }

        [Route ("edit_items")]
        public static async Task<(string, InlineKeyboardMarkup)> EditItems (Update update, int categoryId)
        {
            string ReplyText;
            ReplyText = await Db.GetStringPath (await Db.GetCategory (categoryId));
            Item[] items = await Db.GetItemsByCategory (categoryId);
            int itemCount = (items == null) ? 0 : items.Length;
            ReplyText += $"\nКоличество товаров: {itemCount}";
            return (ReplyText, await Kb.EditItemsInCategory (items, categoryId));
        }

        [Route ("create_item")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateItem (Update update, int categoryId)
        {
            long userId = Router.GetUserId (update);
            string ReplyText = "Введите название товара:";
            await Db.SetAdminStatus (userId, AdminStatus.CreateItemName);
            var item = new Item { CategoryId = categoryId };
            Db.AddToCache (userId, item);
            return (ReplyText, await Kb.Back ($"edit_items?categoryId{categoryId:d10}"));
        }

        [Route ("edit_item")]
        public static async Task<(string, InlineKeyboardMarkup)> EditItem (Update update, int id)
        {
            Item item = await Db.GetItem (id);
            string ReplyText = $"Название: {item.ItemName}\n" +
                        $"Цена: {(await Db.GetGlobalPrice (item.ItemId)).Price}\n" +
                        $"Описание: {item.Description ?? "Пусто"}\n" +
                        $"Категория: {await Db.GetStringPath (item)}\n" +
                        $"Артикул: {item.ItemId:d10}";
            return (ReplyText, await Kb.EditItem (id));
        }

        [Route ("edit_item_name")]
        public static async Task<(string, InlineKeyboardMarkup)> EditItemName (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            Item item = await Db.GetItem (id);
            await Db.SetAdminStatus (userId, AdminStatus.EditItemName);
            Db.AddToCache (userId, item);
            string ReplyText = "Введите новое название товара:";
            return (ReplyText, await Kb.Back ($"edit_item?id={item.ItemId:d10}"));
        }

        [Route ("edit_item_desc")]
        public static async Task<(string, InlineKeyboardMarkup)> EditItemDescription (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            Item item = await Db.GetItem (id);
            await Db.SetAdminStatus (userId, AdminStatus.EditItemDesc);
            Db.AddToCache (userId, item);
            string ReplyText = "Введите новое описание товара:";
            return (ReplyText, await Kb.Back ($"edit_item?id={item.ItemId:d10}"));
        }

        [Route ("delete_item")]
        public static async Task<(string, InlineKeyboardMarkup)> DeleteItem (Update update, int id)
        {
            string ReplyText = string.Empty;
            Item item = await Db.GetItem (id);
            if ( item is not null )
            {
                long userId = Router.GetUserId (update);
                Db.AddToCache (userId, item);
                await Db.SetAdminStatus (userId, AdminStatus.DeleteItem);
                ReplyText = $"Введите название товара (сохраняя регистр) для удаления этого товара:";
            }
            return (ReplyText, await Kb.Back ($"edit_category?id={item.CategoryId:d10}"));
        }

        [Route ("edit_stores")]
        public static async Task<(string, InlineKeyboardMarkup)> EditStores (Update update)
        {
            long userId = Router.GetUserId (update);
            await Db.SetAdminStatus (userId, AdminStatus.Clear);
            Db.RemoveUser (userId);
            return ("Управление магазинами", await Kb.EditStores ());
        }

        [Route ("create_store")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateStore (Update update)
        {
            long userId = Router.GetUserId (update);
            await Db.SetAdminStatus (userId, AdminStatus.CreateStoreName);
            Db.AddToCache (userId, new Store ());
            return ("Введите название магазина:", await Kb.Back ("admin"));
        }

        [Route ("edit_store")]
        public static async Task<(string, InlineKeyboardMarkup)> EditStore (Update update, int id)
        {
            Store store = await Db.GetStore (id);
            string ReplyText = $"Название: {store.StoreName}\n" +
                $"Регион: {store.Region}";
            return (ReplyText, await Kb.EditStore (id));
        }

        [Route ("edit_store_name")]
        public static async Task<(string, InlineKeyboardMarkup)> EditStoreName (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            Db.AddToCache (userId, await Db.GetStore (id));
            await Db.SetAdminStatus (userId, AdminStatus.EditStoreName);
            return ("Введите новое название магазина:", await Kb.Back ($"edit_store?id={id:d10}"));
        }

        [Route ("edit_store_region")]
        public static async Task<(string, InlineKeyboardMarkup)> EditStoreRegion (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            Db.AddToCache (userId, await Db.GetStore (id));
            await Db.SetAdminStatus (userId, AdminStatus.EditStoreRegion);
            return ("Введите новый регион магазина:", await Kb.Back ($"edit_store?id={id:d10}"));
        }

        [Route ("delete_store")]
        public static async Task<(string, InlineKeyboardMarkup)> DeleteStore (Update update, int id)
        {
            string ReplyText = string.Empty;
            Store store = await Db.GetStore (id);
            if ( store is not null )
            {
                long userId = Router.GetUserId (update);
                Db.AddToCache (userId, store);
                await Db.SetAdminStatus (userId, AdminStatus.DeleteStore);
                ReplyText = $"Введите название магазина (сохраняя регистр) для удаления этого магазина:";
            }
            return (ReplyText, await Kb.Back ($"edit_stores"));
        }

        [Route ("create_storeitem")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateStoreItem (Update update, int id)
        {
            return (string.Empty, InlineKeyboardMarkup.Empty ());
        } // !

        [Route ("edit_item_prices")]
        public static async Task<(string, InlineKeyboardMarkup)> EditPrices (Update update, int id)
        {
            return (string.Empty, InlineKeyboardMarkup.Empty ());
        } // !
    }
}
