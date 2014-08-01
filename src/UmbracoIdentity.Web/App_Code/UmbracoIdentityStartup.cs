using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using UmbracoIdentity.Web.Models.UmbracoIdentity;
using UmbracoIdentity.Web;
using Owin;

[assembly: OwinStartup(typeof(UmbracoIdentityStartup))]

namespace UmbracoIdentity.Web
{
   
    /// <summary>
    /// Summary description for Startup
    /// </summary>
    public class UmbracoIdentityStartup
    {
 
        public void Configuration(IAppBuilder app)
        {
            //Single method to configure the Identity user manager for use with Umbraco
            app.ConfigureUserManagerForUmbraco<UmbracoApplicationUser>();

            // Enable the application to use a cookie to store information for the 
            // signed in user and to use a cookie to temporarily store information 
            // about a user logging in with a third party login provider 
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,

                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user 
                    // logs in. This is a security feature which is used when you 
                    // change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator
                        .OnValidateIdentity<UmbracoMembersUserManager<UmbracoApplicationUser>, UmbracoApplicationUser, int>(
                            TimeSpan.FromMinutes(30),
                            (manager, user) => user.GenerateUserIdentityAsync(manager),
                            identity => identity.GetUserId<int>())                            
                }
            });

            //Ensure owin is configured for Umbraco back office authentication - this must
            // be configured AFTER the standard UseCookieConfiguration above.
            app.UseUmbracoBackAuthentication();

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            //// Enables the application to temporarily store user information when 
            //// they are verifying the second factor in the two-factor authentication process.
            //app.UseTwoFactorSignInCookie(
            //    DefaultAuthenticationTypes.TwoFactorCookie,
            //    TimeSpan.FromMinutes(5));

            //// Enables the application to remember the second login verification factor such 
            //// as phone or email. Once you check this option, your second step of 
            //// verification during the login process will be remembered on the device where 
            //// you logged in from. This is similar to the RememberMe option when you log in.
            //app.UseTwoFactorRememberBrowserCookie(
            //    DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Uncomment the following lines to enable logging in 
            // with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            app.UseFacebookAuthentication(
               appId: "296106450571102",
               appSecret: "bde97767dee96e743cbc364bbb39852d");
            
            app.UseGoogleAuthentication(
             clientId: "1072120697051-3hqomjgqnqnra9vt4gr129qr6tfud0fn.apps.googleusercontent.com",
             clientSecret: "QoF1YzsbRXZ-gwbB2fHidcXR");

        }

        
    }
}
