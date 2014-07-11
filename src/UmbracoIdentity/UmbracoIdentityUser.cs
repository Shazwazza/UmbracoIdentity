using System;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
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

    public class UmbracoIdentityUser : IdentityUser<int, IdentityUserLogin<int>, IdentityUserRole<int>, IdentityUserClaim<int>>
    {
        /// <summary>
        /// Gets/sets the members real name
        /// </summary>
        public string Name { get; set; }

    }
}