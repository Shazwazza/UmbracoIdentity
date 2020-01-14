using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security.Cookies;
using System;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;

namespace UmbracoIdentity
{
    /// <summary>
    /// Cookie authentication options for Umbraco front-end authentication
    /// </summary>
    public class FrontEndCookieAuthenticationOptions : CookieAuthenticationOptions
    {
        public FrontEndCookieAuthenticationOptions(
            FrontEndCookieManager frontEndCookieManager,
            ISecuritySection securitySection,
            IGlobalSettings globalSettings)
        {
            CookieManager = frontEndCookieManager;
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
            SlidingExpiration = true;
            ExpireTimeSpan = TimeSpan.FromMinutes(globalSettings.TimeOutInMinutes);
            CookieDomain = securitySection.AuthCookieDomain;
            CookieName = securitySection.AuthCookieName + "_MEMBERS";
            CookieHttpOnly = true;
            CookieSecure = globalSettings.UseHttps ? CookieSecureOption.Always : CookieSecureOption.SameAsRequest;
            CookiePath = "/";

            //NOTE: These are the defaults in the base class
            //ReturnUrlParameter = "ReturnUrl";
            //CookiePath = "/";
            //ExpireTimeSpan = TimeSpan.FromDays(14.0);
            //SlidingExpiration = true;
            //CookieHttpOnly = true;
            //CookieSecure = CookieSecureOption.SameAsRequest;
            //SystemClock = new SystemClock();
            //Provider = new CookieAuthenticationProvider();
        }

        [Obsolete("Use FrontEndCookieAuthenticationOptions constructor with extended parameters")]
        public FrontEndCookieAuthenticationOptions(FrontEndCookieManager frontEndCookieManager)
        {
            CookieManager = frontEndCookieManager;
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie;
        }
    }
}
