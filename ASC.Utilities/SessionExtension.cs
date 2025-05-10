using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ASC.Utilities
{
    public static class SessionExtensions
    {
        public static void SetSession(this ISession session, string key, object value)
        {
            if (value == null) return;
            var jsonData = JsonConvert.SerializeObject(value);
            session.Set(key, Encoding.UTF8.GetBytes(jsonData));
        }

        public static T? GetSession<T>(this ISession session, string key) where T : class
        {
            if (session.TryGetValue(key, out byte[] value))
            {
                var jsonData = Encoding.UTF8.GetString(value);
                return JsonConvert.DeserializeObject<T>(jsonData);
            }
            return null;
        }
    }
}
