using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Umbraco.Core.Configuration;
using Umbraco.Core.Security;
using Umbraco.Web;

namespace UmbracoIdentity
{
    /// <summary>
    /// Used to allow normal Umbraco back office authentication to work
    /// </summary>
    public class UmbracoBackOfficeAuthenticationHandler : AuthenticationHandler<UmbracoBackOfficeAuthenticationOptions>
    {
        /// <summary>
        /// Checks if we should authentication the request (i.e. is back office) and if so gets the forms auth ticket in the request
        /// and returns an AuthenticationTicket based on that.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// It's worth noting that the UmbracoModule still executes and performs the authentication, however this also needs to execute
        /// so that it assigns the new Principal object on the OWIN request:
        /// http://brockallen.com/2013/10/27/host-authentication-and-web-api-with-owin-and-active-vs-passive-authentication-middleware/
        /// </remarks>
        protected override Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            if (ShouldAuthRequest())
            {
                var authTicket = GetAuthTicket(Request, UmbracoConfig.For.UmbracoSettings().Security.AuthCookieName);
                if (authTicket != null)
                {
                    return Task.FromResult(new AuthenticationTicket(new UmbracoBackOfficeIdentity(authTicket), new AuthenticationProperties()));                    
                }                
            }

            return Task.FromResult<AuthenticationTicket>(null);
        }

        private volatile static MethodInfo _shouldAuthRequestMethod = null;
        private bool ShouldAuthRequest()
        {
            //NOTE: we don't care too much about locking/threading here since the result is always the same, if two threads
            // enter this check at the same time it's ok.
            if (_shouldAuthRequestMethod == null)
            {
                //TODO: need to expose ShouldAuthenticateRequest or similar in umb core! using reflection for now
                _shouldAuthRequestMethod = typeof(UmbracoModule).GetMethod("ShouldAuthenticateRequest", BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static);
            }

            if (HttpContext.Current == null)
            {
                return false;
            }

            var httpRequestBase = new HttpRequestWrapper(HttpContext.Current.Request);
            return (bool)_shouldAuthRequestMethod.Invoke(null, new object[] {httpRequestBase, Request.Uri});
        }

        /// <summary>
        /// Returns the current FormsAuth ticket in the request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cookieName"></param>
        /// <returns></returns>
        private static FormsAuthenticationTicket GetAuthTicket(IOwinRequest request, string cookieName)
        {
            if (request == null) throw new ArgumentNullException("request");

            var formsCookie = request.Cookies[cookieName];
            if (formsCookie == null)
            {
                return null;
            }
            //get the ticket
            try
            {
                return FormsAuthentication.Decrypt(formsCookie);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}