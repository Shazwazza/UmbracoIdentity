using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using UmbracoIdentity.Web;
using UmbracoIdentity;

namespace UmbracoIdentity.Web.Models
{
    public class UmbracoApplicationUser : UmbracoIdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<UmbracoApplicationUser, int> manager)
        {
            // Note the authenticationType must match the one 
            // defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity =
                await manager.CreateIdentityAsync(this,
                    DefaultAuthenticationTypes.ApplicationCookie);
            
            // Add custom user claims here
            return userIdentity;
        }
    }
}