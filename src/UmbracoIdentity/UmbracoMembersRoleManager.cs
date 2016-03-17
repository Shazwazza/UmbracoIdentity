using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Umbraco.Core.Services;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    public class UmbracoMembersRoleManager<T> : RoleManager<T, int>
        where T : UmbracoIdentityRole, IRole<int>, new()
    {
        public UmbracoMembersRoleManager(UmbracoMembersRoleStore<T> store)
            : base(store)
        { }

        public Task<IEnumerable<string>> GetAll()
        {
            return ((UmbracoMembersRoleStore<T>) Store).GetAll();
        }

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
