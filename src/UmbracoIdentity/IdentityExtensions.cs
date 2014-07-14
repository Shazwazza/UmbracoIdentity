using System;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using Umbraco.Core;

namespace UmbracoIdentity
{
    public static class IdentityExtensions
    {
        public static T GetUserId<T>(this IIdentity identity)
        {
            var id = identity.GetUserId();
            var a = id.TryConvertTo<T>();
            if (a)
            {
                return a.Result;
            }
            throw new InvalidOperationException("Could not convert the id " + id + " to type " + typeof (T));
        }
    }
}