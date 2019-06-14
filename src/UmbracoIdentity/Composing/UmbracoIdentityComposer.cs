using Umbraco.Core;
using Umbraco.Core.Composing;

namespace UmbracoIdentity.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UmbracoIdentityComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<UmbracoIdentityComponent>();

            composition.Register<IExternalLoginStore, ExternalLoginStore>();

            composition.RegisterUnique<FrontEndCookieManager>();
            composition.Register<FrontEndCookieAuthenticationOptions>();

        }
    }
}
