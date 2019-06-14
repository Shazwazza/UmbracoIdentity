using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using UmbracoIdentity;
using UmbracoIdentity.Web.Models.UmbracoIdentity;
using UmbracoIdentity.Web;
using Owin;
using Umbraco.Web;
using Umbraco.Web.Security;
using Umbraco.Core.Services;

[assembly: OwinStartup("UmbracoIdentityStartup", typeof(UmbracoIdentityOwinStartup))]

namespace UmbracoIdentity.Web
{
   
    /// <summary>
    /// OWIN Startup class for UmbracoIdentity 
    /// </summary>
    public class UmbracoIdentityOwinStartup : UmbracoIdentityOwinStartupBase
    {
        /// <summary>
        /// Configures services to be created in the OWIN context (CreatePerOwinContext)
        /// </summary>
        /// <param name="app"/>
        protected override void ConfigureServices(IAppBuilder app, ServiceContext services)
        {
            base.ConfigureServices(app, services);

            //Single method to configure the Identity user manager for use with Umbraco
            app.ConfigureUserManagerForUmbracoMembers<UmbracoApplicationMember>();

            //Single method to configure the Identity user manager for use with Umbraco
            app.ConfigureRoleManagerForUmbracoMembers<UmbracoApplicationRole>();
        }

        /// <summary>
        /// Configures middleware to be used (i.e. app.Use...)
        /// </summary>
        /// <param name="app"/>
        protected override void ConfigureMiddleware(IAppBuilder app)
        {
            //Ensure owin is configured for Umbraco back office authentication. If you have any front-end OWIN
            // cookie configuration, this must be declared after it.
            app
                .UseUmbracoBackOfficeCookieAuthentication(UmbracoContextAccessor, RuntimeState, Services.UserService, GlobalSettings, UmbracoSettings.Security, PipelineStage.Authenticate)
                .UseUmbracoBackOfficeExternalCookieAuthentication(UmbracoContextAccessor, RuntimeState, GlobalSettings, PipelineStage.Authenticate);

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

            app.UseFacebookAuthentication(
                   appId: "YOURAPPID",
                   appSecret: "YOURAPPSECRET");

            app.UseGoogleAuthentication(
             clientId: "YOURCLIENTID.apps.googleusercontent.com",
             clientSecret: "YOURCLIENTSECRET"); 
#endif


            //Lasty we need to ensure that the preview Middleware is registered, this must come after
            // all of the authentication middleware:
            app.UseUmbracoPreviewAuthentication(UmbracoContextAccessor, RuntimeState, GlobalSettings, UmbracoSettings.Security, PipelineStage.Authorize);

            base.ConfigureMiddleware(app);
        }
        
    }
}
