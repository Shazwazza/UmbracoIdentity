using System.Configuration;
using Umbraco.Core;
using Umbraco.Web.Mvc;

namespace UmbracoIdentity
{
    /// <summary>
    /// Enables the fix for the async action invoker if specified in config
    /// </summary>
    /// <remarks>
    /// Used as a work around for issue: http://issues.umbraco.org/issue/U4-5208
    /// </remarks>
    public class FixAsyncActionInvokerStartupHandler : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            var useFix = ConfigurationManager.AppSettings["UmbracoIdentity:UseAsyncActionInvokerFix"].TryConvertTo<bool>();
            if (useFix && useFix.Result)
            {
                DefaultRenderMvcControllerResolver.Current.SetDefaultControllerType(typeof(FixedAsyncRenderMvcController));

                FilteredControllerFactoriesResolver.Current.RemoveType<RenderControllerFactory>();
                FilteredControllerFactoriesResolver.Current.AddType<FixedAsyncRenderControllerFactory>();

                base.ApplicationStarting(umbracoApplication, applicationContext);   
            }
        }
    }
}