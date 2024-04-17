
namespace TelegramShop.Keyboards
{
    using Telegram.Bot.Types.ReplyMarkups;
    using TelegramShop.DataBase;
    using TelegramShop.Caching;

    public class BtnGenerator
    {
        public static async Task<InlineKeyboardButton[]> GetButton ( string text, string data, bool checkPermission, long userId = 0 )
        {
            if ( checkPermission )
            {
                if ( await Db.HasPermission (userId, data) )
                    return new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: text, callbackData: data) };
                else return Array.Empty<InlineKeyboardButton> ();
            }
            else return new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: text, callbackData: data) }; ;
        }
        public static async Task<InlineKeyboardMarkup> GetOneButtonMarkup (string text, string data) =>
            new InlineKeyboardMarkup (InlineKeyboardButton.WithCallbackData (text, data));
        public static async Task<InlineKeyboardButton[]> Back (string cbData)
            => new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: cbData) };
        public static async Task<InlineKeyboardButton[]> ToAdminMenu ()
            => new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: "В главное меню", callbackData: "admin") };
        public static async Task<InlineKeyboardButton[]> ToMainMenu ()
               => new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: "В главное меню", callbackData: "menu") };
        public static async Task<InlineKeyboardButton> GetBtn (string text, string data)
            => InlineKeyboardButton.WithCallbackData (text: text, callbackData: data);

    }

    public class Kb
    {
        public static async Task<InlineKeyboardMarkup> Admin (long userId)
        {
            return new InlineKeyboardMarkup
            (
                new List<InlineKeyboardButton[]>
                {
                await BtnGenerator.GetButton ("Товары", "edit_catalog", true, userId),
                await BtnGenerator.GetButton ("Заказы", "orders", true, userId),
                await BtnGenerator.GetButton ("Роли", "edit_roles", true, userId),
                await BtnGenerator.GetButton ("Магазины", "edit_stores", true, userId),
                }
            );
        }
        public static async Task<InlineKeyboardMarkup> Menu (long userId)
        {
            int cartCount = await Db.GetItemCountInCart (userId);
            return new InlineKeyboardMarkup
            (
                new List<InlineKeyboardButton[]>
                {
                await BtnGenerator.GetButton ("Каталог", "catalog", false, userId),
                await BtnGenerator.GetButton ($"Корзина ({cartCount})", "cart", false, userId)
                }
            );
        }
        public static async Task<InlineKeyboardMarkup> Catalog (int page, int maxPage)
        {
            var Catalog = new List<InlineKeyboardButton[]> ();
            Category[] categories = await Db.GetCategoriesPage (5, page);
            if ( categories is not null && categories.Length > 0 )
            {
                foreach ( Category c in categories )
                    Catalog.Add (await BtnGenerator.GetButton (c.CategoryName, $"category?id={c.CategoryId:d10}&page={page:d10}", false));
            }
            var Arrows = new List<InlineKeyboardButton> ();
            if ( page > 1 )
                Arrows.Add (await BtnGenerator.GetBtn ("<", $"catalog?page={page - 1:d10}"));
            if ( page < maxPage )
                Arrows.Add (await BtnGenerator.GetBtn (">", $"catalog?page={page + 1:d10}"));
            if ( Arrows.Count > 0 )
                Catalog.Add (Arrows.ToArray ());
            Catalog.Add (await BtnGenerator.Back ("menu"));
            return new InlineKeyboardMarkup (Catalog);
        }
        public static async Task<InlineKeyboardMarkup> CategoriesInCategory (int id, int page)
        {
            var buttons = new List<InlineKeyboardButton[]> ();
            foreach ( Category c in await Db.GetChildCategories (id) )
                buttons.Add (await BtnGenerator.GetButton (c.CategoryName, $"category?id={c.CategoryId:d10}&page={page:d10}", false));
            buttons.Add (await BtnGenerator.Back ($"category?id={(await Db.GetCategory (id)).ParentId:d10}&page={page:d10}"));
            buttons.Add (await BtnGenerator.ToMainMenu ());
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> ItemsInCategory (int id, int page)
        {
            Item[] items = await Db.GetItemsByCategory (id);
            Category c = await Db.GetCategory (id);
            var Category = new List<InlineKeyboardButton[]> ();
            if ( items != null && items.Length > 0 )
            {
                foreach ( Item i in items )
                    Category.Add (await BtnGenerator.GetButton (i.ItemName, $"item?id={i.ItemId:d10}&page={page:d10}", false ));
            }
            Category.Add (await BtnGenerator.Back ($"category?id={c.ParentId:d10}"));
            Category.Add (await BtnGenerator.ToMainMenu ());
            return new InlineKeyboardMarkup (Category);
        }
        public static async Task<InlineKeyboardMarkup> Cart (OrderItem[] items)
        {
            var buttons = new List<InlineKeyboardButton[]> ();
            for (int i = 0; i < items.Length; i++ )
                buttons.Add (new InlineKeyboardButton[]
                {
                    await BtnGenerator.GetBtn ((i+1).ToString (), " "),
                    await BtnGenerator.GetBtn ("Кол-во", $"edit_orderitem_count?id={items[i].Id:d10}"),
                    await BtnGenerator.GetBtn ("Удалить", $"delete_orderitem?id={items[i].Id:d10}")
                });
            if (items.Length > 0 )
                buttons.Add (await BtnGenerator.GetButton ("Оформить заказ", $"make_order?id={items[0].OrderId:d10}", false));
            buttons.Add (await BtnGenerator.ToMainMenu ());
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> Item (int itemId, int page)
        {
            Item item = await Db.GetItem (itemId);
            var buttons = new List<InlineKeyboardButton[]> ();
            buttons.AddRange (new InlineKeyboardButton[][]
            {
                await BtnGenerator.GetButton ("В корзину", $"in_cart?id={itemId:d10}&page={page:d10}", false),
                await BtnGenerator.Back($"category?id={item.CategoryId:d10}")
            });
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> SelectCount (int itemId, byte page)
        {
            InlineKeyboardMarkup Markup = new (new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: $"item {itemId:d10} {page:d3}")
            });
            return Markup;
        }
        public static async Task<InlineKeyboardMarkup> EditCatalog (Category[] categories)
        {
            var Catalog = new List<List<InlineKeyboardButton>> ();
            if ( categories is not null && categories.Length > 0 )
            {
                foreach ( Category c in categories )
                {
                    var categoryButton = InlineKeyboardButton.WithCallbackData (text: c.CategoryName, callbackData: $"edit_category?id={c.CategoryId:d10}");
                    var categoryList = new List<InlineKeyboardButton> { categoryButton };
                    Catalog.Add (categoryList);
                }
            }
            var append = InlineKeyboardButton.WithCallbackData (text: "Добавить категорию", callbackData: $"create_category?parentId={0:d10}");
            var AppendList = new List<InlineKeyboardButton> { append };
            Catalog.Add (AppendList);
            var back = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: "admin");
            var backList = new List<InlineKeyboardButton> { back };
            Catalog.Add (backList);
            return new InlineKeyboardMarkup (Catalog);
        }
        public static async Task<InlineKeyboardMarkup> CreateCategory (int parentId)
        {
            var markup = new InlineKeyboardMarkup (new[]
            {
                InlineKeyboardButton.WithCallbackData(text:"Назад", callbackData: $"edit_category?id={parentId:d10}")
            });

            return markup;
        }
        public static async Task<InlineKeyboardMarkup> EditCategory (int categoryId)
        {
            Category category = await Db.GetCategory (categoryId);
            var CategoryButtons = new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(text: "Подкатегории", callbackData: $"edit_categories?id={category.CategoryId:d10}"),
                InlineKeyboardButton.WithCallbackData(text: "Товары", callbackData: $"edit_items?categoryId={category.CategoryId:d10}")
            };
            if ( await Db.HasCategories (category.CategoryId) )
            {
                CategoryButtons.Remove (CategoryButtons[1]);
                Category[] childCategories = await Db.GetChildCategories (category.CategoryId);
                CategoryButtons[0].Text = $"Подкатегории ({childCategories.Length})";
            }
            else if ( await Db.HasItems (category.CategoryId) )
            {
                CategoryButtons.Remove (CategoryButtons[0]);
                CategoryButtons[0].Text += $" ({await Db.GetItemCountInCategory (category.CategoryId)})";
            }
            List<List<InlineKeyboardButton>> Buttons = new List<List<InlineKeyboardButton>> { CategoryButtons }.Concat (
            new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(text: "Название", callbackData: $"edit_category_name?id={category.CategoryId:d10}")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(text: "Удалить эту категорию", callbackData: $"delete_category?id={category.CategoryId:d10}")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: $"edit_category?id={category.ParentId:d10}")
                }
            }).ToList ();
            var Markup = new InlineKeyboardMarkup (Buttons);
            return Markup;
        }
        public static async Task<InlineKeyboardMarkup> EditCategories (int parentId)
        {
            Category[] categories = await Db.GetChildCategories (parentId);
            var CategoryButtons = new List<List<InlineKeyboardButton>> ();
            foreach ( Category c in categories )
            {
                var categoryButton = InlineKeyboardButton.WithCallbackData (text: c.CategoryName, callbackData: $"edit_category?id={c.CategoryId:d10}");
                var categoryList = new List<InlineKeyboardButton> { categoryButton };
                CategoryButtons.Add (categoryList);
            }
            var addButton = InlineKeyboardButton.WithCallbackData (text: "Добавить подкатегорию", callbackData: $"create_category?parentId={parentId:d10}");
            var addList = new List<InlineKeyboardButton> { addButton };
            var backButton = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: $"edit_category?id={parentId:d10}");
            var backList = new List<InlineKeyboardButton> { backButton };
            CategoryButtons.AddRange (new List<List<InlineKeyboardButton>> { addList, backList });
            return new InlineKeyboardMarkup (CategoryButtons);
        }
        public static async Task<InlineKeyboardMarkup> EditItemsInCategory (Item[] items, int categoryId)
        {
            var Category = new List<List<InlineKeyboardButton>> ();
            if ( items != null && items.Length > 0 )
            {
                foreach ( Item i in items )
                {
                    var item = InlineKeyboardButton.WithCallbackData (text: i.ItemName, callbackData: $"edit_item?id={i.ItemId:d10}");
                    var itemList = new List<InlineKeyboardButton> { item };
                    Category.Add (itemList);
                }
            }
            var back = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: $"edit_category?id={categoryId:d10}");
            var backList = new List<InlineKeyboardButton> { back };
            var append = InlineKeyboardButton.WithCallbackData (text: "Добавить товар", callbackData: $"create_item?categoryId={categoryId:d10}");
            var appendList = new List<InlineKeyboardButton> { append };
            var toMainMenu = InlineKeyboardButton.WithCallbackData (text: "В главное меню", callbackData: "admin");
            var toMainMenuList = new List<InlineKeyboardButton> { toMainMenu };
            Category.AddRange (new List<List<InlineKeyboardButton>> { appendList, backList, toMainMenuList });
            var Markup = new InlineKeyboardMarkup (Category);
            return Markup;
        }
        public static async Task<InlineKeyboardMarkup> EditItem (int itemId)
        {
            Item item = await Db.GetItem (itemId);

            var NameButton = InlineKeyboardButton.WithCallbackData (text: "Изменить название", callbackData: $"edit_item_name?id={item.ItemId:d10}");
            var NameList = new List<InlineKeyboardButton> { NameButton };

            var PriceButton = InlineKeyboardButton.WithCallbackData (text: "Изменить цену", callbackData: $"edit_item_prices?id={item.ItemId:d10}");
            var PriceList = new List<InlineKeyboardButton> { PriceButton };

            var DescButton = InlineKeyboardButton.WithCallbackData (text: "Изменить описание", callbackData: $"edit_item_desc?id={item.ItemId:d10}");
            var DescList = new List<InlineKeyboardButton> { DescButton };

            var CategoryButton = InlineKeyboardButton.WithCallbackData (text: "Изменить категорию", callbackData: $"edit_item_category?id={item.ItemId:d10}");
            var CategoryList = new List<InlineKeyboardButton> { CategoryButton };

            var DeleteButton = InlineKeyboardButton.WithCallbackData (text: "Удалить товар", callbackData: $"delete_item?id={item.ItemId:d10}");
            var DeleteList = new List<InlineKeyboardButton> { DeleteButton };

            var BackButton = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: $"edit_items?categoryId={item.CategoryId:d10}");
            var BackList = new List<InlineKeyboardButton> { BackButton };

            var toMainMenu = InlineKeyboardButton.WithCallbackData (text: "В главное меню", callbackData: "admin");
            var toMainMenuList = new List<InlineKeyboardButton> { toMainMenu };

            var ButtonList = new List<List<InlineKeyboardButton>>
            {
                NameList, PriceList, DescList, CategoryList, DeleteList, BackList, toMainMenuList
            };
            return new InlineKeyboardMarkup (ButtonList);
        }
        public static async Task<InlineKeyboardMarkup> Back (string cbData)
        {
            var markup = new InlineKeyboardMarkup (new[]
            {
                InlineKeyboardButton.WithCallbackData(text:"Назад", callbackData: cbData)
            });
            return markup;
        }
        public static async Task<InlineKeyboardMarkup> EditStores ()
        {
            Store[] Stores = await Db.GetStores ();
            var buttons = new List<List<InlineKeyboardButton>> ();
            foreach ( Store Store in Stores )
            {
                var button = InlineKeyboardButton.WithCallbackData (text: Store.StoreName, callbackData: $"edit_store?id={Store.StoreId:d10}");
                var buttonList = new List<InlineKeyboardButton> { button };
                buttons.Add (buttonList);
            }
            var addButton = InlineKeyboardButton.WithCallbackData (text: "Добавить магазин", callbackData: "create_store");
            var addList = new List<InlineKeyboardButton> { addButton };
            var backButton = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: "admin");
            var backList = new List<InlineKeyboardButton> { backButton };
            buttons.AddRange (new List<List<InlineKeyboardButton>> { addList, backList });
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> EditStore (int storeId)
        {
            var buttons = new List<InlineKeyboardButton[]>
            {
                await BtnGenerator.GetButton (text: "Изменить название", data: $"edit_store_name?id={storeId:d10}", checkPermission: false),
                await BtnGenerator.GetButton (text: "Изменить регион", data: $"edit_store_region?id={storeId:d10}", checkPermission: false)
            };
            if (storeId != 1)
                buttons.Add (await BtnGenerator.GetButton (text: "Удалить магазин", data: $"delete_store?id={storeId:d10}", checkPermission: false));
            buttons.Add (await BtnGenerator.GetButton (text: "Назад", data: "edit_stores", checkPermission: false));
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> EditPrices (int itemId)
        {
            var buttons = new List<InlineKeyboardButton[]> ();
            Item item = await Db.GetItem (itemId);
            StoreItem[] prices = await Db.GetStoreItems (itemId);
            Store[] stores = new Store[prices.Length];
            for ( int i = 0; i < prices.Length; i++ )
                stores[i] = await Db.GetStore (prices[i].StoreId);
            for ( int i = 0; i < stores.Length; i++ )
                buttons.Add (await BtnGenerator.GetButton (text: $"{stores[i].StoreName}: {prices[i].Price}", data: $"edit_item_price?id={prices[i].Id:d10}", checkPermission: false));
            if ( stores.Length < (await Db.GetStores ()).Length )
                buttons.Add (await BtnGenerator.GetButton ("Добавить цену", $"create_item_price?id={itemId:d10}", false));
            buttons.AddRange (new InlineKeyboardButton[][]
            {
                await BtnGenerator.Back ($"edit_item?id={item.ItemId:d10}"),
                await BtnGenerator.ToAdminMenu ()
            });
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> CreateItemPrice (int itemId)
        {
            var buttons = new List<InlineKeyboardButton[]> ();
            Store[] stores = await Db.GetStoresWithoutItem (itemId);
            foreach (Store s in stores)
                buttons.Add (await BtnGenerator.GetButton (s.StoreName, $"create_storeitem?itemId={itemId:d10}&storeId={s.StoreId:d10}", false));
            buttons.AddRange ( new InlineKeyboardButton[][] {
                await BtnGenerator.Back ($"edit_item_prices?id={itemId:d10}"),
                await BtnGenerator.ToAdminMenu ()
            });
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> EditPrice (StoreItem si)
        {
            return new InlineKeyboardMarkup ((new InlineKeyboardButton[][]
            {
                await BtnGenerator.GetButton ("Изменить цену", $"edit_storeitem_price?id={si.Id:d10}", false),
                await BtnGenerator.GetButton ("Изменить количество", $"edit_storeitem_count?id={si.Id:d10}", false),
                await BtnGenerator.GetButton ("Удалить товар из этого магазина", $"delete_storeitem?id={si.Id:d10}", false),
                await BtnGenerator.Back ($"edit_item_prices?id={si.ItemId:d10}"),
                await BtnGenerator.ToAdminMenu ()
            }));
        }
        public static async Task<InlineKeyboardMarkup> EditRoles ()
        {
            Role[] roles = await Db.GetRoles ();
            var buttons = new List<InlineKeyboardButton[]> ();
            foreach ( Role r in roles )
                buttons.Add (await BtnGenerator.GetButton (r.RoleName, $"edit_role?id={r.RoleId:d10}", false));
            buttons.AddRange (new InlineKeyboardButton[][]
            {
                await BtnGenerator.GetButton ("Добавить роль", "create_role", false),
                await BtnGenerator.ToAdminMenu (),
            });
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> CreateRolePermissions (int roleId)
        {
            var buttons = new List<InlineKeyboardButton[]> ();
            foreach (Permission p in Permission.ItemsAndCategories)
                if (await Db.RolePermissionExists (roleId, p.query))
                {
                    buttons.Add (await BtnGenerator.GetButton ("Товары и категории", $"create_rolepermissions_items?roleId={roleId:d10}", false));
                    break;
                }
            foreach ( Permission p in Permission.Orders )
                if ( await Db.RolePermissionExists (roleId, p.query) )
                {
                    buttons.Add (await BtnGenerator.GetButton ("Заказы", $"create_rolepermissions_orders?roleId={roleId:d10}", false));
                    break;
                }
            foreach ( Permission p in Permission.Roles )
                if ( await Db.RolePermissionExists (roleId, p.query) )
                {
                    buttons.Add (await BtnGenerator.GetButton ("Роли", $"create_rolepermissions_roles?roleId={roleId:d10}", false));
                    break;
                }
            foreach ( Permission p in Permission.Stores )
                if ( await Db.RolePermissionExists (roleId, p.query) )
                {
                    buttons.Add (await BtnGenerator.GetButton ("Магазины", $"create_rolepermissions_stores?roleId={roleId:d10}", false));
                    break;
                }
            buttons.Add (await BtnGenerator.Back ("edit_roles"));
            return new InlineKeyboardMarkup (buttons);
        }
        public static async Task<InlineKeyboardMarkup> Orders ()
        {
            var buttons = new List<InlineKeyboardButton[]> ();
            foreach (Order o in await Db.GetOrders ())
                buttons.Add (await BtnGenerator.GetButton ($"№{o.OrderId:d10} - {(long)(DateTime.UtcNow-o.OrderDateTime).TotalMinutes:d} минут назад", $"admin_order?id={o.OrderId:d10}", false));
            buttons.Add (await BtnGenerator.ToAdminMenu ());
            return new InlineKeyboardMarkup (buttons);
        }
        //public static async Task<InlineKeyboardMarkup> CreateRolePermissionsItems (int roleId)
        //{
        //    var buttons = new List<InlineKeyboardButton[]> ();
        //    foreach ( Permission p in Permission.ItemsAndCategories )
        //        buttons.Add (await BtnGenerator.GetButton (p.PermissionName, $"create_rolepermission?roleId={roleId:d10}", false));
        //    buttons.Add (await BtnGenerator.Back ("admin"));
        //    return new InlineKeyboardMarkup (buttons);
        //}
    }
}
