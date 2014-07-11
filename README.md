ASP.Net Identity for Umbraco
===============

Allows for using OWIN &amp; ASP.Net Identity in Umbraco

Currently this is a test project and will allow the use of ASP.Net Identity and OWIN to work for Umbraco members on your front-end website. Eventually I'll try to add support for back office users too (depending on how possible that is with the current forms-auth tickets). 

Some light reading - This is the majority of articles that had to be read and understood to achieve this:

https://delicious.com/shandem/owin

## Minimum Requirements:

Ideally this should be run against Umbraco 7.1.5 or 6.2.2. I realize these versions are not out (based on today's date 11/07/14) yet but in order for this to work, this issue needs to be resolved:

http://issues.umbraco.org/issue/U4-5208

There is a work around in this project which you will need to enable with an AppSetting if you are not using 7.1.5+ or 6.2.2+

    <add key="UmbracoIdentity:UseAsyncActionInvokerFix" value="true"/>

This project is built against .Net 4.5.1

## Installation

First, read the minimum requirements above as you might need to enable the work around.

### Config updates

Remove FormsAuthentication from your web.config, this should be the last entry in your httpmodules lists:

    <remove name="FormsAuthenticationModule" />
    
You then need to disable FormsAuthentication as the authentication provider, change the authentication element to:

    <authentication mode="None" />
    
Replace the 'type' attribute of your UmbracoMembershipProvider in your web.config to be:

    "UmbracoIdentity.IdentityEnabledMembersMembershipProvider, UmbracoIdentity"
    
### Owin setup

**STOP!!** If you are not familiar with OWIN or ASP.Net Identity you will need to stop here and familiarize yourself with these things. If you are running the latest version of Visual Studio 2013, then you can create a new web project and create a site that has authentication. This will give you a good example of how to setup OWIN and Authentication. The 'light reading' link above also has tons of information.

If you are familiar then here's what to do... In your OWIN startup class:

* Create an ApplicationUser class - just like the one that is created in the VS templates. It just needs to inherit from `Microsoft.AspNet.Identity.EntityFramework.IdentityUser`
* Register an `UmbracoMembersUserManager<T>` in the OWIN context:

        app.CreatePerOwinContext<UmbracoMembersUserManager<ApplicationUser>>(
            (o, c) => UmbracoMembersUserManager<ApplicationUser>
                .Create(o, c, ApplicationContext.Current.Services.MemberService));
                
* Setup the default cookie authentication, this is basically the same as the VS template but you'll use an UmbracoMembersUserManager instead with your ApplicationUser:

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
                    .OnValidateIdentity<UmbracoMembersUserManager<ApplicationUser>, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user)
                            => user.GenerateUserIdentityAsync(manager))
            }
        });
        
* Next you need to enable Umbraco back office authentication, otherwise the default cookie authorization above will try to auth the back office user which we don't want

        app.UseUmbracoBackAuthentication();
        
## Usage

Because this is using a different authentication mechanism than Umbraco normally uses, it means that a few of the MembershipHelper methods will not work, such as sign in/out. You will need to use OWIN to perform these methods. With this setup you should be able to use the code that you'll find in the VS template's AccountController.
    
