[![Build status](https://ci.appveyor.com/api/projects/status/qaiwo2w8k5ieaf3v?svg=true)](https://ci.appveyor.com/project/Shandem/umbracoidentity)

![ASP.Net Identity for Umbraco](logo.png?raw=true)
===============

[![NuGet](https://img.shields.io/nuget/v/Nuget.Core.svg)](https://www.nuget.org/packages/UmbracoIdentity)

Allows for using OWIN &amp; ASP.Net Identity in Umbraco for front-end members

This project will allow the use of ASP.Net Identity and OWIN to work for Umbraco members on your front-end website. It is compatible with your current members and how passwords are currently hashed - so long as your membership provider is configured for hashing passwords (default) and not encrypting them.

There are some **[known issues and limitations](https://github.com/Shandem/UmbracoIdentity/wiki/Known-Issues)**

## Minimum Requirements:

This package requires **Umbraco 7.6.0+**. 

*[If you are upgrading UmbracoIdentity to a new major version you will need to read these instructions!](https://github.com/Shazwazza/UmbracoIdentity/wiki/Upgrading)*

## Installation

### Nuget

    PM> Install-Package UmbracoIdentity

There's also a nightly Nuget feed: `https://ci.appveyor.com/nuget/umbracoidentity-nightly`

*There is also just an UmbracoIdentity.Core package too in case you just want the binaries*

    PM> Install-Package UmbracoIdentity.Core

### Config updates

These config updates 'should' be taken care of by the nuget install, but you should double check to be sure.

Remove FormsAuthentication from your web.config, this should be the last entry in your httpmodules lists:

```xml
<remove name="FormsAuthenticationModule" />
```

The entry is slightly different for the system.webServer/modules list:

```xml
<remove name="FormsAuthentication" />
```

You then need to disable FormsAuthentication as the authentication provider, change the authentication element to:

```xml
<authentication mode="None" />
```
    
Replace the 'type' attribute of your UmbracoMembershipProvider in your web.config to be:

```xml
"UmbracoIdentity.IdentityEnabledMembersMembershipProvider, UmbracoIdentity"
```

### Umbraco integration

You'll need to configure some Umbraco properties: https://github.com/Shazwazza/UmbracoIdentity/wiki#umbraco-integration

### Owin setup

[*What is OWIN? If you are unsure, be sure to read this before you continue!*](https://github.com/Shazwazza/UmbracoIdentity/wiki/What-is-Owin)

If you are familiar with OWIN then here's what to do... 

* `Models/UmbracoApplicationMember` - this is similar to the ApplicationUser class that comes with the VS 2013 template, except that this one inherits from UmbracoIdentityMember. You can customize this how you like.
* `App_Start/UmbracoIdentityStartup` - this is the OWIN startup class that will be used
* In your web.config, change the appSetting `owin:appStartup` to: `UmbracoIdentityStartup`
* The `UmbracoIdentityStartup` file is now your OWIN startup class, you can modify it if you require Identity customizations. If you want to enable 3rd party OAuth authentication, you'll need to follow the normal ASP.Net Identity documentation, sample code for this exists in the `UmbracoIdentityStartup` class.

## [Documentation](https://github.com/Shandem/UmbracoIdentity/wiki)

See docs for examples, usage, etc...

## Copyright & Licence

&copy; 2016 by Shannon Deminick

This is free software and is licensed under the [MIT License](http://opensource.org/licenses/MIT)
