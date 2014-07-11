using Umbraco.Web.Mvc;

namespace UmbracoIdentity
{
    /// <summary>
    /// Used as a work around for issue: http://issues.umbraco.org/issue/U4-5208
    /// </summary>
    public class FixedAsyncRenderMvcController : RenderMvcController
    {
        public FixedAsyncRenderMvcController()
        {
            this.ActionInvoker = new FixedAsyncRenderActionInvoker();
        }
    }
}