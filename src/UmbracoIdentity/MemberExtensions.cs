using System.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;

namespace UmbracoIdentity
{
    internal static class MemberExtensions
    {
        

        /// <summary>
        /// Checks if the security stamp property exists and if so returns it, otherwise null
        /// </summary>
        /// <param name="member"></param>
        /// <param name="propertyExists">returns false if the property </param>
        /// <returns></returns>
        public static string GetSecurityStamp(this IMember member, out bool propertyExists)
        {
            propertyExists = Current.Services.MemberTypeService.Get(member.ContentTypeId).PropertyTypeExists(UmbracoIdentityConstants.SecurityStampProperty);
            if (!propertyExists)
                return null;

            if (!member.Properties.Contains(UmbracoIdentityConstants.SecurityStampProperty))
                return null;

            return member.Properties[UmbracoIdentityConstants.SecurityStampProperty]?.GetValue()?.ToString() ?? string.Empty;
        }
    }
}