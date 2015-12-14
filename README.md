![ASP.Net Identity for Umbraco](logo.png?raw=true)
===============

Allows for using OWIN &amp; ASP.Net Identity in Umbraco for front-end members

This project will allow the use of ASP.Net Identity and OWIN to work for Umbraco members on your front-end website. It is compatible with your current members and how passwords are currently hashed - so long as your membership provider is configured for hashing passwords (default) and not encrypting them.

There are some **[known issues and limitations](https://github.com/Shandem/UmbracoIdentity/wiki/Known-Issues)**

## Minimum Requirements:

This package is built against Umbraco 7.2.4 and the Nuget dependency is also for that version though it will probably work with umbraco versions 7.1.6+ or 6.2.2+

If you are running previous versions to 7.1.6 or 6.2.2 then in order for this to work you will need to apply a work around and enable this AppSetting:

    <add key="UmbracoIdentity:UseAsyncActionInvokerFix" value="true"/>

## Installation

First, read the minimum requirements above as you might need to enable the work around.

### Nuget

    PM> Install-Package UmbracoIdentity

### Config updates

These config updates 'should' be taken care of by the nuget install, but you should double check to be sure.

Remove FormsAuthentication from your web.config, this should be the last entry in your httpmodules lists:

    <remove name="FormsAuthenticationModule" />
    
The entry is slightly different for the system.webServer/modules list:

    <remove name="FormsAuthentication" />
    
You then need to disable FormsAuthentication as the authentication provider, change the authentication element to:

    <authentication mode="None" />
    
Replace the 'type' attribute of your UmbracoMembershipProvider in your web.config to be:

    "UmbracoIdentity.IdentityEnabledMembersMembershipProvider, UmbracoIdentity"
    
### Owin setup

[*What is OWIN? If you are unsure, be sure to read this before you continue!*](https://github.com/Shazwazza/UmbracoIdentity/wiki/Legacy-Installation)

If you are familiar then here's what to do... 

#### If you are using Umbraco 7.3+

Once you've installed this Nuget package you should install this one:

    PM> Install-Package UmbracoCms.IdentityExtensions

you will see some classes added to your App_Start & Models folders:

* UmbracoApplicationMember - this is similar to the ApplicationUser class that comes with the VS 2013 template, except that this one inherits from UmbracoIdentityMember. You can customize this how you like.
* UmbracoIdentityStartup - With Umbraco 7.3+ you won't need to worry about this class
* UmbracoStandardOwinStartup - This is installed as part of the `UmbracoCms.IdentityExtensions` package which we will use to enable the UmbracoIdentity engine.

Here's what you need to do:

* In your web.config, change the appSetting `owin:appStartup` to: `UmbracoStandardOwinStartup`
* In the `UmbracoStandardOwinStartup` file, add these lines of code:

    ```csharp
    //Single method to configure the Identity user manager for use with Umbraco
    app.ConfigureUserManagerForUmbracoMembers<UmbracoApplicationMember>();
    
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
                .OnValidateIdentity<UmbracoMembersUserManager<UmbracoApplicationMember>, UmbracoApplicationMember, int>(
                    TimeSpan.FromMinutes(30),
                    (manager, user) => user.GenerateUserIdentityAsync(manager),
                    UmbracoIdentity.IdentityExtensions.GetUserId<int>)
        }
    });
    ```
    
If you want to enable 3rd party OAuth authentication, you'll need to follow the normal ASP.Net Identity documentation, sample code for this exists in the `UmbracoStartup` class installed in your App_Start folder.

#### If you are using Umbraco < 7.3

Once you've installed the Nuget package, you will see some classes added to your App_Start folder:

* UmbracoApplicationUser - this is similar to the ApplicationUser class that comes with the VS 2013 template, except that this one inherits from UmbracoIdentityMember. You can customize this how you like.
* UmbracoStartup - this is basically the same as the Startup class that comes with the the VS 2013 template, except that it is named UmbracoStartup and contains some slightly different extension method calls:

    ```csharp
    //Single method to configure the Identity user manager for use with Umbraco
    app.ConfigureUserManagerForUmbracoMembers<UmbracoApplicationMember>();
    
    //Ensure owin is configured for Umbraco back office authentication
    app.UseUmbracoBackAuthentication();
    ```

    * The rest of the startup class is pretty much the same as the VS 2013 template except that you are using your UmbracoApplicationMember type and the UmbracoMembersUserManager class.

## [Documentation](https://github.com/Shandem/UmbracoIdentity/wiki)

See docs for examples, usage, etc...
