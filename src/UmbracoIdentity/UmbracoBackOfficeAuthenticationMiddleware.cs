using Microsoft.Owin;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace UmbracoIdentity
{
    ///// <summary>
    ///// Used to enable the normal Umbraco back office authentication to operate
    ///// </summary>
    //public class UmbracoBackOfficeAuthenticationMiddleware : AuthenticationMiddleware<UmbracoBackOfficeAuthenticationOptions>
    //{
    //    public UmbracoBackOfficeAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, UmbracoBackOfficeAuthenticationOptions options)
    //        : base(next, options) 
    //    {
    //    }

    //    protected override AuthenticationHandler<UmbracoBackOfficeAuthenticationOptions> CreateHandler()
    //    {
    //        return new UmbracoBackOfficeAuthenticationHandler();
    //    }
    //}
}
