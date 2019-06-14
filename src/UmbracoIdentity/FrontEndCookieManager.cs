using System;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using Microsoft.Owin.Infrastructure;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Web;

namespace UmbracoIdentity
{
    /// <summary>
    /// A custom cookie manager that is used to read the cookie from the request.
    /// </summary> 
    /// <remarks>
    /// Umbraco's back office cookie is read for specific paths, for the front-end we only want to read cookies for paths that Umbraco is
    /// not going to authenticate for. By doing this we ensure that only one cookie authenticator is executed for a request.
    /// </remarks>
    public class FrontEndCookieManager : ChunkingCookieManager, ICookieManager
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IRuntimeState _runtime;
        private readonly IGlobalSettings _globalSettings;

        public FrontEndCookieManager(IUmbracoContextAccessor umbracoContextAccessor, IRuntimeState runtime, IGlobalSettings globalSettings)
        {
            _umbracoContextAccessor = umbracoContextAccessor ?? throw new ArgumentNullException(nameof(umbracoContextAccessor));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _globalSettings = globalSettings ?? throw new ArgumentNullException(nameof(globalSettings));
        }

        /// <summary>
        /// Explicitly implement this so that we filter the request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        string ICookieManager.GetRequestCookie(IOwinContext context, string key)
        {
            if (_umbracoContextAccessor.UmbracoContext == null || IsClientSideRequest(context.Request.Uri))
            {
                return null;
            }

            var authForBackOffice = ShouldAuthForBackOfficeRequest(context);

            return authForBackOffice
                //Don't auth request since this is for the back office, don't return a cookie
                ? null
                //Return the default implementation
                : base.GetRequestCookie(context, key);
        }
        
        private static bool IsClientSideRequest(Uri url)
        {
            var ext = Path.GetExtension(url.LocalPath);
            if (ext.IsNullOrWhiteSpace()) return false;
            var toInclude = new[] { ".aspx", ".ashx", ".asmx", ".axd", ".svc" };
            return toInclude.Any(ext.InvariantEquals) == false;
        }

        private static bool IsBackOfficeRequest(IOwinRequest request, IGlobalSettings globalSettings)
        {
            return (bool) typeof (UriExtensions).CallStaticMethod("IsBackOfficeRequest", request.Uri, HttpRuntime.AppDomainAppVirtualPath, globalSettings);
        }

        private static bool IsInstallerRequest(IOwinRequest request)
        {
            return (bool)typeof(UriExtensions).CallStaticMethod("IsInstallerRequest", request.Uri);
        }
        
        /// <summary>
        /// Determines if the request should be authenticated for the back office
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        /// <remarks>
        /// We auth the request when:
        /// * it is a back office request
        /// * it is an installer request
        /// * it is a preview request
        /// </remarks>
        private bool ShouldAuthForBackOfficeRequest(IOwinContext ctx)
        {
            if (_runtime.Level == RuntimeLevel.Install)
                return false;

            var request = ctx.Request;
            
            if (//check back office
                IsBackOfficeRequest(request, _globalSettings)
                    //check installer
                || IsInstallerRequest(request))
            {
                return true;
            }
            return false;
        }



    }
}
