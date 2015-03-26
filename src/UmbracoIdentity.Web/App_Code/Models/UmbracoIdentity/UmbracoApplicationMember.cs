using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using UmbracoIdentity;

namespace UmbracoIdentity.Web.Models.UmbracoIdentity
{
    public class UmbracoApplicationMember : UmbracoIdentityMember
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<UmbracoApplicationMember, int> manager)
        {
            // Note the authenticationType must match the one 
            // defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            
            // Add custom user claims here
            return userIdentity;
        }
    }
}