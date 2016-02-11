using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Security.Providers;
using Task = System.Threading.Tasks.Task;

namespace UmbracoIdentity
{
    public class UmbracoMembersRoleStore<T> : DisposableObject, IRoleStore<T, int>
        where T : UmbracoIdentityRole, IRole<int>,  new ()
    {
        private readonly IMemberService _memberService;
        private readonly IMemberGroupService _memberGroupService;
        private readonly MembersRoleProvider _roleProvider;

        public UmbracoMembersRoleStore(
            IMemberService memberService,
            IMemberGroupService memberGroupService,
            MembersRoleProvider roleProvider)
        {
            _memberService = memberService;
            _memberGroupService = memberGroupService;
            _roleProvider = roleProvider;
        }

        public Task CreateAsync(T role)
        {
            var memberGroup = new MemberGroup { Name = role.Name };
            _memberGroupService.Save(memberGroup);

            _roleProvider.CreateRole(role.Name);

            throw new NotImplementedException();
        }

        public Task UpdateAsync(T role)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T role)
        {
            throw new NotImplementedException();
        }

        public Task<T> FindByIdAsync(int roleId)
        {
            throw new NotImplementedException();
        }

        public Task<T> FindByNameAsync(string roleName)
        {
            throw new NotImplementedException();
        }

        protected override void DisposeResources()
        {
            throw new NotImplementedException();
        }
    }
}
