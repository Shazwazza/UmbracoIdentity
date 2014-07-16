using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using UmbracoIdentity.Web.Models;
using UmbracoIdentity.Web;
using Owin;

//NOTE: Here's most of the info I had to read to get all this done: https://delicious.com/shandem/owin
using Umbraco.Core;
using Umbraco.Core.ObjectResolution;
using UmbracoIdentity;

[assembly: OwinStartup(typeof(Startup))]

namespace UmbracoIdentity.Web
{
    /// <summary>
    /// Summary description for Startup
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure the db context, user manager and role 
            // manager to use a single instance per request
            //app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<UmbracoMembersUserManager<ApplicationUser>>(
                (o, c) => UmbracoMembersUserManager<ApplicationUser>
                    .Create(o, c, ApplicationContext.Current.Services.MemberService));

            //app.CreatePerOwinContext<ApplicationRoleManager>(ApplicationRoleManager.Create);

            // Enable the application to use a cookie to store information for the 
            // signed in user and to use a cookie to temporarily store information 
            // about a user logging in with a third party login provider 
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,

                //TODO: You'd adjust your cookie settings accordingly
                //LoginPath = new PathString("/Account/Login"),

                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user 
                    // logs in. This is a security feature which is used when you 
                    // change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator
                        .OnValidateIdentity<UmbracoMembersUserManager<ApplicationUser>, ApplicationUser, int>(
                            TimeSpan.FromMinutes(30),
                            (manager, user) => user.GenerateUserIdentityAsync(manager),
                            identity => identity.GetUserId<int>())                            
                }
            });

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

            //app.UseFacebookAuthentication(
            //   appId: "",
            //   appSecret: "");

            //able-winter-638

            app.UseGoogleAuthentication(
             clientId: "1072120697051-3hqomjgqnqnra9vt4gr129qr6tfud0fn.apps.googleusercontent.com",
             clientSecret: "QoF1YzsbRXZ-gwbB2fHidcXR");
        }
    }
}
