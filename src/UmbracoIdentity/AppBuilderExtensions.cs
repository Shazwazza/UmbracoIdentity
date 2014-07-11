using System;
using Microsoft.Owin.Extensions;
using Owin;

namespace UmbracoIdentity
{
    public static class AppBuilderExtensions
    {
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