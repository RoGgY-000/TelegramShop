﻿
namespace TelegramShop.Keyboards
{
    using Telegram.Bot.Types.ReplyMarkups;
    using TelegramShop.DataBase;
    using TelegramShop.Caching;

    public class ButtonGenerator
    {
        public static async Task<InlineKeyboardButton[]?> GetButton ( string text, string data, bool checkPermission, long userId = 0 )
        {
            if ( checkPermission )
            {
                if ( await Db.HasPermission (userId, data) )
                    return new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: text, callbackData: data) };
                else return null;
            }
            else return new InlineKeyboardButton[] { InlineKeyboardButton.WithCallbackData (text: text, callbackData: data) }; ;
        }
        public static async Task<InlineKeyboardMarkup> GetOneButtonMarkup (string text, string data) =>
            new InlineKeyboardMarkup (InlineKeyboardButton.WithCallbackData (text, data));

    }

    public class Kb
    {
        public static async Task<InlineKeyboardMarkup> Admin (long userId)
        {
            return new InlineKeyboardMarkup
            (
                new List<InlineKeyboardButton[]>
                {
                await ButtonGenerator.GetButton ("Товары", "edit_catalog", true, userId),
                await ButtonGenerator.GetButton ("Заказы", "orders", true, userId),
                await ButtonGenerator.GetButton ("Роли", "roles", true, userId),
                await ButtonGenerator.GetButton ("Магазины", "edit_stores", true, userId),
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
                await ButtonGenerator.GetButton ("Каталог", "catalog", false, userId),
                await ButtonGenerator.GetButton ($"Корзина ({cartCount})", "cart", false, userId)
                }
            );
        }
        public static async Task<InlineKeyboardMarkup> Catalog (Category[]? categories)
        {
            var Catalog = new List<List<InlineKeyboardButton>> ();
            if ( categories is not null && categories.Length > 0 )
            {
                foreach ( Category c in categories )
                {
                    var categoryButton = InlineKeyboardButton.WithCallbackData (text: c.CategoryName, callbackData: $"category {c.CategoryId:d10}");
                    var categoryList = new List<InlineKeyboardButton> { categoryButton };
                    Catalog.Add (categoryList);
                }
            }
            var back = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: "menu");
            var backList = new List<InlineKeyboardButton> { back };
            Catalog.Add (backList);
            return new InlineKeyboardMarkup (Catalog);

        }
        public static async Task<InlineKeyboardMarkup> ItemsInCategory (Item[] items)
        {
            var Category = new List<List<InlineKeyboardButton>> ();
            if ( items != null && items.Length > 0 )
            {
                Category category = await Db.GetCategory (items[0].CategoryId);
                foreach ( Item i in items )
                {
                    var itemButton = InlineKeyboardButton.WithCallbackData (text: i.ItemName, callbackData: $"item?id={i.ItemId:d10}");
                    var itemList = new List<InlineKeyboardButton> { itemButton };
                    Category.Add (itemList);
                }
                var back = InlineKeyboardButton.WithCallbackData (text: "Назад", callbackData: $"category?{items[0].CategoryId:d10}");
                var backList = new List<InlineKeyboardButton> { back };
                var toMainMenu = InlineKeyboardButton.WithCallbackData (text: "В главное меню", callbackData: "menu");
                var toMainMenuList = new List<InlineKeyboardButton> { toMainMenu };
                Category.AddRange (new List<List<InlineKeyboardButton>> { backList, toMainMenuList });
            }
            return new InlineKeyboardMarkup (Category);
        }
        public static async Task<InlineKeyboardMarkup> Cart (long userId)
        {
            return null;
        }
        public static async Task<InlineKeyboardMarkup> Item (int itemId, byte page)
        {
            var item = await Db.GetItem (itemId);
            InlineKeyboardMarkup Markup = new (new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "В корзину", callbackData: $"in_cart {itemId:d10} {page:d3}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(text: "Назад", callbackData: $"catalog {page}")
            }
            });
            return Markup;
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
            var Markup = new InlineKeyboardMarkup (Catalog);
            return Markup;
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
            var buttons = new List<InlineKeyboardButton[]> ();
            buttons.Add (await ButtonGenerator.GetButton (text: "Изменить название", data: $"edit_store_name?id={storeId:d10}", checkPermission: false));
            buttons.Add (await ButtonGenerator.GetButton (text: "Изменить регион", data: $"edit_store_region?id={storeId:d10}", checkPermission: false));
            if (storeId != 1)
                buttons.Add (await ButtonGenerator.GetButton (text: "Удалить магазин", data: $"delete_store?id={storeId:d10}", checkPermission: false));
            buttons.Add (await ButtonGenerator.GetButton (text: "Назад", data: "edit_stores", checkPermission: false));
            return new InlineKeyboardMarkup (buttons);
        }
        //public static async Task<InlineKeyboardMarkup> EditPrices (int itemId)
        //{
        //    Item item = await Db.GetItem (itemId);
            
        //    var buttons = new List<InlineKeyboardButton[]> ();
            
        //}
    }
}
