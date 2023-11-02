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
            Category[] CategoryArray = await Db.GetRootCategories();
            await Db.SetAdminStatus(update.CallbackQuery.From.Id, AdminStatus.Clear);
            if (CategoryArray == null || CategoryArray.Length < 1)
                ReplyText = "К сожалению, пока наш каталог пуст :(";
            else if (CategoryArray is not null && CategoryArray.Length > 0)
                ReplyText = $"Выберите категорию:\n";
            return (ReplyText, await Kb.EditCatalog (CategoryArray));
        }
    }
}
