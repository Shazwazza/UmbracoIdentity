using Umbraco.Core;
using Umbraco.Core.Composing;

namespace UmbracoIdentity.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Install)]
    public class UmbracoIdentityComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            //manually check runtime state, don't add our component unless we are in the running state
            if (composition.RuntimeState.Level >= RuntimeLevel.Run)
            {
                composition.Components().Append<UmbracoIdentityComponent>();
                composition.Register<IExternalLoginStore, ExternalLoginStore>();
            }   

            composition.RegisterUnique<FrontEndCookieManager>();
            composition.Register<FrontEndCookieAuthenticationOptions>();

        }
    }
}
