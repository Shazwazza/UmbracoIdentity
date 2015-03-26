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

**STOP!!** If you are not familiar with OWIN or ASP.Net Identity you will need to stop here and familiarize yourself with these things. If you are running the latest version of Visual Studio 2013, then you can create a new web project and create a site that has authentication. This will give you a good example of how to setup OWIN and Authentication. The following link above also has tons of information for you to start leaning with: https://delicious.com/shandem/owin

If you are familiar then here's what to do... 

Once you've installed the Nuget package, you will see some classes added to your App_Start folder:

* UmbracoApplicationUser - this is similar to the ApplicationUser class that comes with the VS 2013 template, except that this one inherits from UmbracoIdentityMember. You can customize this how you like.
* UmbracoStartup - this is basically the same as the Startup class that comes with the the VS 2013 template, except that it is named UmbracoStartup and contains some slightly different extension method calls:

        //Single method to configure the Identity user manager for use with Umbraco
        app.ConfigureUserManagerForUmbracoMembers<UmbracoApplicationMember>();
        
        //Ensure owin is configured for Umbraco back office authentication
        app.UseUmbracoBackAuthentication();

    * The rest of the startup class is pretty much the same as the VS 2013 template except that you are using your UmbracoApplicationMember type and the UmbracoMembersUserManager class.

## [Documentation](https://github.com/Shandem/UmbracoIdentity/wiki)

See docs for examples, usage, etc...
