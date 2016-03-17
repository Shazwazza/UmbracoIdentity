using System;
using Microsoft.Owin;
using Owin;
using Umbraco.Core;
using Microsoft.AspNet.Identity.Owin;
using UmbracoIdentity.Models;

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
                    appContext.Services.MemberTypeService,
                    appContext.Services.MemberGroupService,
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


        /// <summary>
        /// Configure Identity Role Manage for Umbraco
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app">The application.</param>
        /// <param name="appContext">The application context.</param>
        public static void ConfigureRoleManagerForUmbracoMembers<T>(
            this IAppBuilder app,
            ApplicationContext appContext = null) where T : UmbracoIdentityRole, new()
        {
            if (appContext == null) appContext = ApplicationContext.Current;
            if (appContext == null) throw new ArgumentNullException("appContext");

            //Don't proceed if the app is not ready
            if (!appContext.IsConfigured
                || appContext.DatabaseContext == null
                || !appContext.DatabaseContext.IsDatabaseConfigured) return;

            //Configure Umbraco members role manager to be created per request
            app.CreatePerOwinContext<UmbracoMembersRoleManager<T>>(((o, c) => UmbracoMembersRoleManager<T>.Create(
                o,
                appContext.Services.MemberGroupService)));
        }

        /// <summary>
        /// Configures the role manager for umbraco members.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="app">The application.</param>
        /// <param name="customRoleStore">The custom role store.</param>
        public static void ConfigureRoleManagerForUmbracoMembers<T>(
            this IAppBuilder app,
            UmbracoMembersRoleStore<T> customRoleStore) 
            where T : UmbracoIdentityRole, new()
        {
            //Configure Umbraco members role manager to be created per request
            app.CreatePerOwinContext<UmbracoMembersRoleManager<T>>(((o, c) => UmbracoMembersRoleManager<T>.Create(
                o,
                customRoleStore)));
        }

        /// <summary>
        /// Configures the role manager for umbraco members.
        /// </summary>
        /// <typeparam name="TManager">The type of the manager.</typeparam>
        /// <typeparam name="TRole">The type of the role.</typeparam>
        /// <param name="app">The application.</param>
        /// <param name="roleManager">The role manager.</param>
        public static void ConfigureRoleManagerForUmbracoMembers<TManager, TRole>(
            this IAppBuilder app,
            Func<IdentityFactoryOptions<TManager>, IOwinContext, TManager> roleManager)
            where TManager : UmbracoMembersRoleManager<TRole>
            where TRole : UmbracoIdentityRole, new()
        {
            //Configure Umbraco members role manager to be created per request
            app.CreatePerOwinContext<TManager>(roleManager);
        }
    }
}