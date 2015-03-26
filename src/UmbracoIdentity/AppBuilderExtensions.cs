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
        public static void ConfigureUserManagerForUmbracoMembers<T>(this IAppBuilder app)
            where T : UmbracoIdentityMember, new()
        {

            //Don't proceed if the app is not ready
            if (!ApplicationContext.Current.IsConfigured
                || ApplicationContext.Current.DatabaseContext == null
                || !ApplicationContext.Current.DatabaseContext.IsDatabaseConfigured) return;

            //Configure Umbraco user manager to be created per request
            app.CreatePerOwinContext<UmbracoMembersUserManager<T>>(
                (o, c) => UmbracoMembersUserManager<T>.Create(
                    o, c, ApplicationContext.Current.Services.MemberService));

            //Configure Umbraco member event handler to be created per request - this will ensure that the
            // external logins are kept in sync if members are deleted from Umbraco
            app.CreatePerOwinContext<MembersEventHandler<T>>((options, context) => new MembersEventHandler<T>(context));

        }

        /// <summary>
        /// Ensures that the UmbracoBackOfficeAuthenticationMiddleware is assigned to the pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IAppBuilder UseUmbracoBackAuthentication(this IAppBuilder app)
        {
            if (app == null) throw new ArgumentNullException("app");
            app.Use(typeof (UmbracoBackOfficeAuthenticationMiddleware),
                app,
                new UmbracoBackOfficeAuthenticationOptions());               
            app.UseStageMarker(PipelineStage.Authenticate);
            return app;
        }

    }
}