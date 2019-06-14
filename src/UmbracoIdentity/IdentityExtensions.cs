using System;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using Umbraco.Core;

namespace UmbracoIdentity
{
    public static class IdentityExtensions
    {
        public static bool TryGetUserId<T>(this IIdentity identity, out T userId)
        {
            var id = identity.GetUserId();
            var a = id.TryConvertTo<T>();
            if (a)
            {
                userId = a.Result;
                return true;
            }
            userId = default(T);
            return false;
        }
    }
}