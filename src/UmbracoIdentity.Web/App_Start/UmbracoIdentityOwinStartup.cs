using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Configuration;
using System.Linq;
using UmbracoIdentity.Web;
using UmbracoIdentity.Web.Models.UmbracoIdentity;

[assembly: OwinStartup("UmbracoIdentityStartup", typeof(UmbracoIdentityOwinStartup))]
namespace UmbracoIdentity.Web
{

    /// <summary>
    /// OWIN Startup class for UmbracoIdentity 
    /// </summary>
    public class UmbracoIdentityOwinStartup : UmbracoIdentityOwinStartupBase
    {
        protected override void ConfigureUmbracoUserManager(IAppBuilder app)
        {
            base.ConfigureUmbracoUserManager(app);

            //Single method to configure the Identity user manager for use with Umbraco
            app.ConfigureUserManagerForUmbracoMembers<UmbracoApplicationMember>();

            //Single method to configure the Identity user manager for use with Umbraco
            app.ConfigureRoleManagerForUmbracoMembers<UmbracoApplicationRole>();
        }

        protected override void ConfigureUmbracoAuthentication(IAppBuilder app)
        {
            base.ConfigureUmbracoAuthentication(app);

            // Enable the application to use a cookie to store information for the 
            // signed in user and to use a cookie to temporarily store information 
            // about a user logging in with a third party login provider 
            // Configure the sign in cookie
            var cookieOptions = CreateFrontEndCookieAuthenticationOptions();
            cookieOptions.Provider = new CookieAuthenticationProvider
            {
                // Enables the application to validate the security stamp when the user 
                // logs in. This is a security feature which is used when you 
                // change a password or add an external login to your account.  
                OnValidateIdentity = SecurityStampValidator
                        .OnValidateIdentity<UmbracoMembersUserManager<UmbracoApplicationMember>, UmbracoApplicationMember, int>(
                            TimeSpan.FromMinutes(30),
                            (manager, user) => user.GenerateUserIdentityAsync(manager),
                            identity => identity.GetUserId<int>())
            };

            app.UseCookieAuthentication(cookieOptions, PipelineStage.Authenticate);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            //app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            //app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            //app.UseMicrosoftAccountAuthentication(
            //  clientId: "",
            //  clientSecret: "");

            //app.UseTwitterAuthentication(
            //  consumerKey: "",
            //  consumerSecret: "");

            //app.UseFacebookAuthentication(
            //  appId: "",
            //  appSecret: "");

            //app.UseGoogleAuthentication(
            //  clientId: "",
            //  clientSecret: "");

#if DEBUG

            //NOTE: THIS block gets removed during the build process, this is for internal dev purposes

            bool HasConfig(params string[] keys) => keys.All(x => ConfigurationManager.AppSettings[x] != null);

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

            if (HasConfig("UmbracoIdentity.OAuth.Facebook.AppId", "UmbracoIdentity.OAuth.Facebook.AppSecret"))
                app.UseFacebookAuthentication(
                    appId: ConfigurationManager.AppSettings["UmbracoIdentity.OAuth.Facebook.AppId"],
                    appSecret: ConfigurationManager.AppSettings["UmbracoIdentity.OAuth.Facebook.AppSecret"]);

            if (HasConfig("UmbracoIdentity.OAuth.Google.ClientId", "UmbracoIdentity.OAuth.Google.Secret"))
                app.UseGoogleAuthentication(
                    clientId: ConfigurationManager.AppSettings["UmbracoIdentity.OAuth.Google.ClientId"],
                    clientSecret: ConfigurationManager.AppSettings["UmbracoIdentity.OAuth.Google.Secret"]);
#endif
        }
    }
}
