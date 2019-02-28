using Umbraco.Core;
using Umbraco.Core.Composing;

namespace UmbracoIdentity.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class UmbracoIdentityComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            //composition.Register<IdentityComponent>();
            composition.Register<IExternalLoginStore, ExternalLoginStore>();

            composition.Components().Append<UmbracoIdentityComponent>();
        }
    }
}
