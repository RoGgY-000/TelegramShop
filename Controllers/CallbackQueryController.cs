namespace TelegramShop.Controllers
{
    using System.Text;
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
            await Db.RemoveUser (userId);
            if ( CategoryArray == null || CategoryArray.Length < 1 )
                ReplyText = "К сожалению, пока наш каталог пуст :(";
            else
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
            await Db.AddToCache (userId, item);
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

        [Route ("create_item_price")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateStoreItem (Update update, int id)
        {
            return ("Выберите магазин:", await Kb.CreateItemPrice (id));
        }

        [Route ("edit_item_prices")]
        public static async Task<(string, InlineKeyboardMarkup)> EditPrices (Update update, int id)
        {
            Item item = await Db.GetItem (id);
            return ($"Цены товара {item.ItemName}:", await Kb.EditPrices (id));
        }

        [Route ("create_storeitem")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateStoreItem (Update update, int itemId, int storeId)
        {
            long userId = Router.GetUserId (update);
            var si = new StoreItem { ItemId = itemId, StoreId = storeId };
            await Db.AddToCache (userId, si);
            await Db.SetAdminStatus (userId, AdminStatus.CreateItemLocalPrice);
            return ("Введите цену:", InlineKeyboardMarkup.Empty ());
        }

        [Route ("edit_item_price")]
        public static async Task<(string, InlineKeyboardMarkup)> EditItemPrice (Update update, int id)
        {
            StoreItem? si = await Db.GetStoreItem (id);
            if ( si is not null )
            {
                Item item = await Db.GetItem (si.ItemId);
                string ReplyText = $"Название: {item.ItemName}\n" +
                        $"Описание: {item.Description ?? "Пусто"}\n" +
                        $"Категория: {await Db.GetStringPath (item)}\n" +
                        $"Артикул: {item.ItemId:d10}\n" +
                        $"Цена: {si.Price}\n" +
                        $"Количество: {si.Count}";
                return (ReplyText, await Kb.EditPrice (si));
            }
            else return ("Ошибка", InlineKeyboardMarkup.Empty ());
        }

        [Route ("edit_storeitem_price")]
        public static async Task<(string, InlineKeyboardMarkup)> EditStoreItemPrice (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            var si = await Db.GetStoreItem (id);
            await Db.SetAdminStatus (userId, AdminStatus.EditStoreItemPrice);
            await Db.AddToCache (userId, si);
            return ("Введите новую цену:", InlineKeyboardMarkup.Empty ());
        }

        [Route ("edit_storeitem_count")]
        public static async Task<(string, InlineKeyboardMarkup)> EditStoreItemCount (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            var si = await Db.GetStoreItem (id);
            await Db.SetAdminStatus (userId, AdminStatus.EditStoreItemCount);
            await Db.AddToCache (userId, si);
            return ("Введите новое количество:", InlineKeyboardMarkup.Empty ());
        }

        [Route ("edit_roles")]
        public static async Task<(string, InlineKeyboardMarkup)> EditRoles (Update update)
        {
            await Db.SetAdminStatus (update.CallbackQuery.From.Id, AdminStatus.Clear);
            return ("Роли:", await Kb.EditRoles ());
        }

        [Route ("create_role")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateRole (Update update)
        {
            await Db.SetAdminStatus (update.CallbackQuery.From.Id, AdminStatus.CreateRoleName);
            return ("Введите название роли:", InlineKeyboardMarkup.Empty ());
        }

        [Route ("create_rolepermissions_items")]
        public static async Task<(string, InlineKeyboardMarkup)> CreateRolePermission (Update update, int roleId)
        {
            foreach ( Permission p in Permission.ItemsAndCategories )
                if ( !await Db.RolePermissionExists (roleId, p.query) )
                    await Db.CreateRolePermission (roleId, p.query);
            return (update.Message.Text ?? string.Empty, await Kb.CreateRolePermissions (roleId));
        }
        //[Route ("create_rolepermissions_items")]
        //public static async Task<(string, InlineKeyboardMarkup)> CreateRolePermissionsItems (Update update, int roleId)
        //{
        //    return ("Выберите разрешения:", await Kb.CreateRolePermissionsItems (roleId));
        //}

        [Route (true, "catalog")]
        public static async Task<(string, InlineKeyboardMarkup)> Catalog (Update update, int page = 1)
        {
            int maxPage = await Db.GetCategoriesCount (0) / 5 + 1;
            return ($"Каталог:\nСтраница {page} из {maxPage}", await Kb.Catalog (page, maxPage));
        }

        [Route (true, "category")]
        public static async Task<(string, InlineKeyboardMarkup)> Category (Update update, int id, int page = 1)
        {
            if ( id > 0 )
            {
                Category c = await Db.GetCategory (id);
                return await Db.HasItems (id)
                    ? (await Db.GetStringPath (c), await Kb.ItemsInCategory (id, page))
                    : (await Db.GetStringPath (c), await Kb.CategoriesInCategory (id, page));
            }
            else 
                return await Catalog (update, page);
        }

        [Route (true, "item")]
        public static async Task<(string, InlineKeyboardMarkup)> Item (Update update, int id, int page = 1)
        {
            Item item = await Db.GetItem (id);
            await Db.RemoveUser (update.CallbackQuery.From.Id);
            string replytext = $"{await Db.GetStringPath (item)}\n" +
                $"{item.ItemName}\n" +
                $"{item.Description}\n" +
                $"Цена: {(await Db.GetGlobalPrice (id)).Price}\n" +
                $"<b>Цена может отличаться в зависимости от города/региона.\nТочная цена будет указана при оформлении заказа</b>\n" +
                $"Артикул: {item.ItemId}";
            return (replytext, await Kb.Item (item.ItemId, page));
        }

        [Route (true, "in_cart")]
        public static async Task<(string, InlineKeyboardMarkup)> InCart (Update update, int id, int page)
        {
            long userId = Router.GetUserId (update);
            Item item = await Db.GetItem (id);
            Db.SelectCountCache.AddPair (userId, item);
            return ("Введите количество:", await BtnGenerator.GetOneButtonMarkup ("Назад", $"item?id={item.ItemId}&page={page:d10}"));
        }

        [Route (true, "cart")]
        public static async Task<(string, InlineKeyboardMarkup)> Cart (Update update)
        {
            long userId = Router.GetUserId (update);
            await Db.RemoveUser (userId);
            OrderItem[] items = await Db.GetUserCart (userId);
            StringBuilder replyText = new ("Ваша корзина:\n");
            int summ = 0;
            for ( int i = 0; i <items.Length; i++ )
            {
                Item item = await Db.GetItem (items[i].ItemId);
                replyText.Append ($"<b>{i+1}</b> {item.ItemName}\n" +
                    $"    Цена за шт: {items[i].Price / items[i].Count}, {items[i].Count} шт.\n" +
                    $"    Итого - {items[i].Price}\n");
                summ += items[i].Price;
            }
            replyText.Append ($"Сумма заказа: {summ}");
            return (replyText.ToString (), await Kb.Cart (items));
        }

        [Route (true, "delete_orderitem")]
        public static async Task <(string, InlineKeyboardMarkup)> DeleteOrderItem (Update update, int id)
        {
            await Db.DeleteOrderItem (id);
            return await Cart (update);
        }

        [Route (true, "edit_orderitem_count")]
        public static async Task <(string, InlineKeyboardMarkup)> EditOrderItemCount (Update update, int id)
        {
            long userId = Router.GetUserId (update);
            OrderItem item = await Db.GetOrderItem (id);
            Db.SelectCountCache.AddPair (userId, item);
            return ("Введите новое количество:", await BtnGenerator.GetOneButtonMarkup ("Назад", "cart"));
        }

        [Route (true, "make_order")]
        public static async Task<(string, InlineKeyboardMarkup)> MakeOrder (Update update, int id)
        {
            await Db.SetOrderStatus (id, OrderStatus.Created);
            return ("Заказ оформлен, ожидайте сообщения от менеджера с уточненнием деталей доставки", await Kb.Back ("menu"));
        }

        [Route ("orders")]
        public static async Task<(string, InlineKeyboardMarkup)> Orders (Update update)
        {
            return ("Текущие заказы:", await Kb.Orders ());
        }

        //[Route ("admin_order")]
        //public static async Task<(string, InlineKeyboardMarkup)> AdminOrder (Update update, int id)
        //{
        //    Order order = await Db.GetOrder (id);
        //    StringBuilder replyText = new ($"Заказ №{order.OrderId}\n" +
        //        $"Заказчик: t.me/{order.UserId}");
        //    return (string.Empty, InlineKeyboardMarkup.Empty ());
        //}
    }
}
