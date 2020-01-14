[![Build status](https://ci.appveyor.com/api/projects/status/qaiwo2w8k5ieaf3v?svg=true)](https://ci.appveyor.com/project/Shandem/umbracoidentity)

![ASP.Net Identity for Umbraco](logo.png?raw=true)
===============

[![NuGet](https://img.shields.io/nuget/v/UmbracoIdentity.svg)](https://www.nuget.org/packages/UmbracoIdentity)

Allows for using OWIN &amp; ASP.Net Identity in Umbraco for front-end members

This project will allow the use of ASP.Net Identity and OWIN to work for Umbraco members on your front-end website. It is compatible with your current members and how passwords are currently hashed - so long as your membership provider is configured for hashing passwords (default) and not encrypting them.

There are some **[known issues and limitations](https://github.com/Shandem/UmbracoIdentity/wiki/Known-Issues)**

## Minimum Requirements:

This package requires **Umbraco 7.6.0+**. 

*[If you are upgrading UmbracoIdentity to a new major version you will need to read these instructions!](https://github.com/Shazwazza/UmbracoIdentity/wiki/Upgrading)*

## Installation

The package can be installed with Nuget:

    PM> Install-Package UmbracoIdentity
   
Then follow the [3 installation steps](https://github.com/Shazwazza/UmbracoIdentity/wiki#installation) and you will end up with code files installed complete with examples and an integrated demo page that looks like:

![UmbracoIdentity demo page](https://github.com/Shazwazza/UmbracoIdentity/blob/master/assets/demo-page.png "UmbracoIdentity demo page")

## [Documentation](https://github.com/Shandem/UmbracoIdentity/wiki)

See docs for further details, integration notes, examples, usage, etc...

## Copyright & Licence

&copy; 2020 by Shannon Deminick

This is free software and is licensed under the [MIT License](http://opensource.org/licenses/MIT)
