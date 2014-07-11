using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Web.Mvc;

namespace UmbracoIdentity
{
    /// <summary>
    /// Used as a work around for issue: http://issues.umbraco.org/issue/U4-5208
    /// </summary>
    public class FixedAsyncRenderControllerFactory : RenderControllerFactory
    {
        public override IController CreateController(RequestContext requestContext, string controllerName)
        {
            var controller1 = base.CreateController(requestContext, controllerName);
            var controller2 = controller1 as Controller;
            if (controller2 != null)
                controller2.ActionInvoker = new FixedAsyncRenderActionInvoker();
            return controller1;
        }
    }
}