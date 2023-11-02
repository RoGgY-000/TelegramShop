
namespace TelegramShop.Routing
{
    using System.Reflection;
    using System.Text;
    using Telegram.Bot.Types.ReplyMarkups;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using TelegramShop.DataBase;
    using TelegramShop.Caching;
    using TelegramShop.Attributes;
    using TelegramShop.Controllers;

    internal class Router
    {
        private static readonly Dictionary<string, MethodInfo> Methods = new ();

        private const string badChars = "'\"*&^%$#@!{}[]`~;\\|=+<>?№/";

        public static void Init ()
        {
            Type[] controllers = Assembly.GetExecutingAssembly ().GetTypes ().Where (t => t.Namespace == "TelegramShop.Controllers").ToArray ();
            foreach ( Type t in controllers )
            {
                foreach ( MethodInfo method in t.GetMethods () )
                {
                    foreach ( object obj in method.GetCustomAttributes (attributeType: typeof (RouteAttribute), inherit: false) )
                    {
                        var attr = (RouteAttribute) obj;
                        foreach (string route in attr.Routes)
                            Methods.Add (route, method);
                    }
                }
            }
        }
        public static async Task<(string, InlineKeyboardMarkup)> GetResponseAsync (Update update)
        {
            (string, InlineKeyboardMarkup)? result = null;
            switch ( update.Type )
            {
                case UpdateType.Message:
                    if ( update.Message is not null
                        && update.Message.Text is not null
                        && update.Message.From is not null )
                    {
                        string text = RemoveBadChars (update.Message.Text);
                        if ( Methods.ContainsKey (text)
                            && Methods.TryGetValue (text, out MethodInfo? Method)
                            && Method is not null )
                            result = await (Task<(string, InlineKeyboardMarkup)>) Method.Invoke (obj: Method, parameters: new object[] { update });
                        else if ( Methods.TryGetValue ("default", out MethodInfo? Default)
                            && Default is not null )
                            result = await (Task<(string, InlineKeyboardMarkup)>) Default.Invoke (obj: Default, parameters: new object[] { update });
                    }
                    break;
                case UpdateType.CallbackQuery:
                    if ( update.CallbackQuery is not null
                        && update.CallbackQuery.Data is not null )
                    {
                        string data = update.CallbackQuery.Data;
                        string parameters = string.Empty;
                        if ( data.Contains ('?') )
                        {
                            parameters = data[data.IndexOf ('?')..];
                        }
                        string query = data.Contains('?') ? data[..data.IndexOf ('?')] : data;
                        if ( Methods.ContainsKey (query)
                        && Methods.TryGetValue (query, out MethodInfo? Method)
                        && Method is not null )
                        { 
                            ParameterInfo[] Parameters = Method.GetParameters ();
                            object[] ParametersValues = new object[Parameters.Length];
                            ParametersValues[0] = update;
                            if ( Parameters.Length > 1 )
                            {
                                for ( int i = 0; i < Parameters.Length; i++ )
                                {
                                    if ( Parameters[i].HasDefaultValue )
                                        ParametersValues[i] = Parameters[i].DefaultValue;
                                    else if ( Parameters[i].Name is not null
                                        && parameters.Contains (Parameters[i].Name) )
                                    {
                                        int ValueIndex = parameters.IndexOf (Parameters[i].Name) + Parameters[i].Name.Length + 1;
                                        if ( int.TryParse (parameters.AsSpan (ValueIndex, 10), out int value) )
                                        {
                                            ParametersValues[i + 1] = value;
                                        }
                                    }
                                }
                            }
                            result = await (Task<(string, InlineKeyboardMarkup)>) Method.Invoke (obj: Method, parameters: ParametersValues);
                        }
                    }
                    break;
            }
            return result is ValueTuple<string, InlineKeyboardMarkup> tuple
                ? tuple
                : throw new ArgumentException ();
        }
        public static string RemoveBadChars (string text)
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
        public static long GetUserId (Update update)
        {
            switch ( update.Type )
            {
                case UpdateType.Message:
                    return update.Message.From.Id;
                case UpdateType.CallbackQuery:
                    return update.CallbackQuery.From.Id;
                default:
                    return 0;
            }
        }
        public static string GetQuery (Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    return update.Message.Text;
                case UpdateType.CallbackQuery:
                    string data = update.CallbackQuery.Data?? string.Empty;
                    return RemoveBadChars (data.Contains ('?') ? data[..data.IndexOf ('?')] : data);
                default:
                    return string.Empty;
            }
        }
    }
}
