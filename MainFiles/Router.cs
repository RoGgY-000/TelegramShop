
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
        private static readonly Dictionary<string, MethodInfo> FreeAccess = new ();

        private const string badChars = "'\"{}[]\\|+<>";

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
                        foreach ( string route in attr.Routes )
                        {
                            Methods.Add (route, method);
                            if ( attr.WithoutPermission )
                                FreeAccess.Add (route, method);
                        }
                    }
                }
            }
        }

        public static async Task<(string, InlineKeyboardMarkup)> GetResponseAsync (Update update)
        {
            try
            {
                (string, InlineKeyboardMarkup)? result = null;
                switch ( update.Type )
                {
                    case UpdateType.Message:
                        string text = RemoveBadChars (update.Message.Text);
                        if ( Methods.ContainsKey (text)
                            && Methods.TryGetValue (text, out MethodInfo? method)
                            && method is not null )
                            result = await (Task<(string, InlineKeyboardMarkup)>) method.Invoke (obj: method, parameters: new object[] { update });
                        else if ( Methods.TryGetValue ("default", out MethodInfo? Default)
                            && Default is not null )
                            result = await (Task<(string, InlineKeyboardMarkup)>) Default.Invoke (obj: Default, parameters: new object[] { update });
                        break;
                    case UpdateType.CallbackQuery:
                        if ( update.CallbackQuery is not null
                            && update.CallbackQuery.Data is not null )
                        {
                            string data = update.CallbackQuery.Data;
                            string parameters = string.Empty;
                            for ( int i = 0; i < data.Length; i++ )
                                if ( data[i] == '?' )
                                {
                                    parameters = data[(i + 1)..];
                                    data = data[..i];
                                }
                            string query = data.Contains ('?') ? data[..data.IndexOf ('?')] : data;
                            MethodInfo Method;
                            if ( FreeAccess.ContainsKey (query)
                                && FreeAccess.TryGetValue (query, out MethodInfo? method2))
                                Method = method2;
                            else
                            {
                                if ( await Db.HasPermission (GetUserId (update), query) )
                                {
                                    Methods.TryGetValue (query, out MethodInfo? method1);
                                    Method = method1;
                                }
                                else return ("Недостаточно прав!", InlineKeyboardMarkup.Empty ());
                            }
                            if ( Method is not null )
                            {
                                ParameterInfo[] Parameters = Method.GetParameters ();
                                object[] ParametersValues = new object[Parameters.Length];
                                ParametersValues[0] = update;
                                if ( Parameters.Length > 1 )
                                {
                                    for ( int i = 1; i < Parameters.Length; i++ )
                                    {
                                        if ( Parameters[i].Name is not null
                                            && parameters.Contains (Parameters[i].Name) )
                                        {
                                            int ValueIndex = parameters.IndexOf (Parameters[i].Name) + Parameters[i].Name.Length + 1;
                                            if ( int.TryParse (parameters.AsSpan (ValueIndex, 10), out int value) )
                                            {
                                                ParametersValues[i] = value;
                                            }
                                        }
                                        else if ( Parameters[i].HasDefaultValue )
                                            ParametersValues[i] = Parameters[i].DefaultValue;
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
            catch (Exception e)
            { Console.WriteLine (e);
                return ("Ошибка", InlineKeyboardMarkup.Empty ()); }
        }

        //private static (string, ParameterInfo[]) GetQueryWithParameters (string query, ParameterInfo[] args)
        //{
        //    bool hasParameters = false;
        //    string parameters;
        //    for ( int i = 0; i < query.Length; i++ )
        //        if ( query[i] == '?' )
        //        {
        //            parameters = query[(i + 1)..];
        //            query = query[..i];
        //            hasParameters = true;
        //        }
        //    if ( !hasParameters )
        //        return (query, Array.Empty<ParameterInfo>());
        //    else
        //    {
        //        int x = 0;
        //        for (int i = 0; i<query.Length; i++)
        //        {
        //            if ( "?&".Contains (query[i]) )
        //                x = i;
        //            else if ( query[i] == '=' )

        //        }
        //    }

        //}
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
                    return RemoveBadChars (update.Message.Text);
                case UpdateType.CallbackQuery:
                    string data = update.CallbackQuery.Data?? string.Empty;
                    return RemoveBadChars (data.Contains ('?') ? data[..data.IndexOf ('?')] : data);
                default:
                    return string.Empty;
            }
        }
    }
}
