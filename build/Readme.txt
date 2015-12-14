UmbracoIdentity for Umbraco has been installed. 

You will need to read the documentation in order to get started:

https://github.com/Shandem/UmbracoIdentity

Some files/folder worth knowing about:

* ~/App_Start/UmbracoIdentityStartup.cs
* ~/Controllers/UmbracoIdentityAccountController.cs
* ~/Models/UmbracoIdentity/*.cs

If you want to use the third party identity providers such as Google, Microsoft, etc... 
logins you will need to install those packages separately:

Install-Package Microsoft.Owin.Security.Facebook

Install-Package Microsoft.Owin.Security.Google

Install-Package Microsoft.Owin.Security.MicrosoftAccount

Install-Package Microsoft.Owin.Security.Twitter

etc....