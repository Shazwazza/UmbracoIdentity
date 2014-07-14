using Microsoft.AspNet.Identity.EntityFramework;

namespace UmbracoIdentity
{
    public class UmbracoIdentityUser : IdentityUser<int, IdentityUserLogin<int>, IdentityUserRole<int>, IdentityUserClaim<int>>
    {
        /// <summary>
        /// Gets/sets the members real name
        /// </summary>
        public string Name { get; set; }

    }
}