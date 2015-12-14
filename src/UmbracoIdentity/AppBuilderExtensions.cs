using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Owin;
using Umbraco.Core;
using Microsoft.AspNet.Identity.Owin;

namespace UmbracoIdentity
{
    public static class AppBuilderExtensions
    {


        /// <summary>
        /// Configure Identity User Manager for Umbraco
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <param name="appContext"></param>
        /// <param name="membershipProvider"></param>
        public static void ConfigureUserManagerForUmbracoMembers<T>(this IAppBuilder app,
            ApplicationContext appContext = null,
            IdentityEnabledMembersMembershipProvider membershipProvider = null)
            where T : UmbracoIdentityMember, new()
        {
            if (appContext == null) appContext = ApplicationContext.Current;
            if (appContext == null) throw new ArgumentNullException("appContext");

            //Don't proceed if the app is not ready
            if (!appContext.IsConfigured
                || appContext.DatabaseContext == null
                || !appContext.DatabaseContext.IsDatabaseConfigured) return;

            //Configure Umbraco user manager to be created per request
            app.CreatePerOwinContext<UmbracoMembersUserManager<T>>(
                (o, c) => UmbracoMembersUserManager<T>.Create(
                    o,
                    appContext.Services.MemberService,
                    membershipProvider: membershipProvider));

            //Configure Umbraco member event handler to be created per request - this will ensure that the
            // external logins are kept in sync if members are deleted from Umbraco
            app.CreatePerOwinContext<MembersEventHandler<T>>((options, context) => new MembersEventHandler<T>(context));
        }

        /// <summary>
        /// Configure identity User Manager for Umbraco with custom user store
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app"></param>
        /// <param name="customUserStore"></param>
        /// <param name="appContext"></param>
        /// <param name="membershipProvider"></param>
        public static void ConfigureUserManagerForUmbracoMembers<T>(this IAppBuilder app,
            UmbracoMembersUserStore<T> customUserStore,
            ApplicationContext appContext,
            IdentityEnabledMembersMembershipProvider membershipProvider = null)
            where T : UmbracoIdentityMember, new()
        {            
            if (appContext == null) throw new ArgumentNullException("appContext");

            //Don't proceed if the app is not ready
            if (!appContext.IsConfigured
                || appContext.DatabaseContext == null
                || !appContext.DatabaseContext.IsDatabaseConfigured) return;

            //Configure Umbraco user manager to be created per request
            app.CreatePerOwinContext<UmbracoMembersUserManager<T>>(
                (o, c) => UmbracoMembersUserManager<T>.Create(
                    o,
                    customUserStore,
                    membershipProvider));

            //Configure Umbraco member event handler to be created per request - this will ensure that the
            // external logins are kept in sync if members are deleted from Umbraco
            app.CreatePerOwinContext<MembersEventHandler<T>>((options, context) => new MembersEventHandler<T>(context));
        }

        /// <summary>
        /// Configure custom identity User Manager for Umbraco
        /// </summary>
        /// <typeparam name="TManager"></typeparam>
        /// <typeparam name="TUser"></typeparam>
        /// <param name="app"></param>
        /// <param name="appContext"></param>
        /// <param name="userManager"></param>
        public static void ConfigureUserManagerForUmbracoMembers<TManager, TUser>(this IAppBuilder app,
            ApplicationContext appContext,
            Func<IdentityFactoryOptions<TManager>, IOwinContext, TManager> userManager)
            where TManager : UmbracoMembersUserManager<TUser>
            where TUser : UmbracoIdentityMember, new()
        {
            if (appContext == null) throw new ArgumentNullException("appContext");
            if (userManager == null) throw new ArgumentNullException("userManager");

            //Don't proceed if the app is not ready
            if (appContext.IsConfigured == false
                || appContext.DatabaseContext == null
                || appContext.DatabaseContext.IsDatabaseConfigured == false) return;

            //Configure Umbraco user manager to be created per request
            app.CreatePerOwinContext<TManager>(userManager);
        }
        

    }
}