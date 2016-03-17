using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using UmbracoIdentity.Models;
using Task = System.Threading.Tasks.Task;

namespace UmbracoIdentity
{
    public class UmbracoMembersRoleStore<T> : DisposableObject, IRoleStore<T, int>
        where T : UmbracoIdentityRole, IRole<int>,  new ()
    {
        private readonly IMemberGroupService _memberGroupService;

        public UmbracoMembersRoleStore(
            IMemberGroupService memberGroupService)
        {
            _memberGroupService = memberGroupService;
        }

        public Task<IEnumerable<string>> GetAll()
        {
            return Task.FromResult(_memberGroupService.GetAll().Select(x => x.Name));
        }

        public Task CreateAsync(T role)
        {
            if (role == null) throw new ArgumentNullException("role");

            var memberGroup = new MemberGroup
            {
                Name = role.Name
            };

            _memberGroupService.Save(memberGroup);

            role.Id = memberGroup.Id;

            return Task.FromResult(0);
        }

        public Task UpdateAsync(T role)
        {
            if (role == null) throw new ArgumentNullException("role");

            var memberGroup = _memberGroupService.GetById(role.Id);
            if (memberGroup != null)
            {
                if (MapToMemberGroup(role, memberGroup))
                {
                    _memberGroupService.Save(memberGroup);
                }
            }

            return Task.FromResult(0);
        }

        public Task DeleteAsync(T role)
        {
            if (role == null) throw new ArgumentNullException("role");

            var memberGroup = _memberGroupService.GetById(role.Id);
            if (memberGroup != null)
            {
                _memberGroupService.Delete(memberGroup);
            }

            return Task.FromResult(0);
        }

        public Task<T> FindByIdAsync(int roleId)
        {
            var memberGroup = _memberGroupService.GetById(roleId);

            return Task.FromResult(memberGroup == null
                ? (T)null
                : MapFromMemberGroup(memberGroup));
        }

        public Task<T> FindByNameAsync(string roleName)
        {
            var memberGroup = _memberGroupService.GetByName(roleName);

            return Task.FromResult(memberGroup == null
                ? (T)null
                : MapFromMemberGroup(memberGroup));
        }

        protected override void DisposeResources()
        {
            // Dispose any resources here...
        }

        private T MapFromMemberGroup(IMemberGroup memberGroup)
        {
            var result = new T
            {
                Id = memberGroup.Id,
                Name = memberGroup.Name
            };

            return result;
        }

        private bool MapToMemberGroup(T role, IMemberGroup memberGroup)
        {
            var anythingChanged = false;

            if (memberGroup.Name != role.Name && role.Name.IsNullOrWhiteSpace() == false)
            {
                memberGroup.Name = role.Name;
                anythingChanged = true;
            }

            return anythingChanged;
        }
    }
}
