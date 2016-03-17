using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Umbraco.Core.Services;

namespace UmbracoIdentity
{
    public class UmbracoMembersRoleManager<T> : RoleManager<T, int>
        where T : UmbracoIdentityRole, IRole<int>, new()
    {
        public UmbracoMembersRoleManager(IRoleStore<T, int> store)
            : base(store)
        { }

        public static UmbracoMembersRoleManager<T> Create(IdentityFactoryOptions<UmbracoMembersRoleManager<T>> options, 
            IMemberGroupService memberGroupService)
        {
            var roleStore = new UmbracoMembersRoleStore<T>(memberGroupService);
            return Create(options, roleStore);
        }

        public static UmbracoMembersRoleManager<T> Create(IdentityFactoryOptions<UmbracoMembersRoleManager<T>> options,
            UmbracoMembersRoleStore<T> roleStore)
        {
            return new UmbracoMembersRoleManager<T>(roleStore);
        } 
    }
}
