using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Umbraco.Core.Services;
using Umbraco.Web.Security.Providers;

namespace UmbracoIdentity
{
    public class UmbracoMembersRoleManager<T> : RoleManager<T, int>
        where T : UmbracoIdentityRole, IRole<int>, new()
    {
        public UmbracoMembersRoleManager(IRoleStore<T, int> store)
            : base(store)
        { }

        public static UmbracoMembersRoleManager<T> Create(IdentityFactoryOptions<UmbracoMembersRoleManager<T>> options, 
            IMemberService memberService,
            IMemberGroupService memberGroupService,
            MembersRoleProvider roleProvider = null)
        {
            var roleStore = new UmbracoMembersRoleStore<T>(memberService, memberGroupService);
            return new UmbracoMembersRoleManager<T>(roleStore);
        } 
    }
}
