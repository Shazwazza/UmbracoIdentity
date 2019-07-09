
using Umbraco.Core;
using Umbraco.Web;

namespace UmbracoIdentity
{
    public class UmbracoIdentityOwinStartupBase : UmbracoDefaultOwinStartup
    {
        protected FrontEndCookieAuthenticationOptions CreateFrontEndCookieAuthenticationOptions() => Umbraco.Core.Composing.Current.Factory.GetInstance<FrontEndCookieAuthenticationOptions>();        
    }
}
