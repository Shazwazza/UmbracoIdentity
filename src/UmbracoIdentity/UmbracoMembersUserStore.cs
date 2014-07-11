using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace UmbracoIdentity
{
    /// <summary>
    /// A custom user store that uses Umbraco member data
    /// </summary>
    public class UmbracoMembersUserStore<T> : IUserStore<T>, IUserPasswordStore<T> 
        where T : IdentityUser, IUser<string>, new()
    {
        private readonly IMemberService _memberService;
        private readonly IdentityEnabledMembersMembershipProvider _membershipProvider;

        public UmbracoMembersUserStore(IMemberService memberService, IdentityEnabledMembersMembershipProvider membershipProvider)
        {
            _memberService = memberService;
            _membershipProvider = membershipProvider;

            if (_membershipProvider.PasswordFormat != MembershipPasswordFormat.Hashed)
            {
                throw new InvalidOperationException("Cannot use ASP.Net Identity with UmbracoMembersUserStore when the password format is not Hashed");
            }
        }

        public Task CreateAsync(T user)
        {
            return Task.Run(() =>
            {
                //MembershipCreateStatus status;

                //var member = _membershipProvider.CreateUser(
                //    user.UserName,
                //    user.PasswordHash,
                //    user.Email,
                //    "", "", true, null, out status);

                //if (status != MembershipCreateStatus.Success)
                //{
                //    throw new ApplicationException("Could not create user, status: " + status);
                //}
            });
        }

        public Task UpdateAsync(T user)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T user)
        {
            throw new NotImplementedException();
        }

        public Task<T> FindByIdAsync(string userId)
        {
            var attempt = userId.TryConvertTo<int>();
            if (attempt)
            {
                var member = _memberService.GetById(attempt.Result);
                if (member == null)
                {
                    return Task.FromResult<T>(null);
                }
                return Task.FromResult(new T
                {
                    Email = member.Email,
                    Id = member.Id.ToString(CultureInfo.InvariantCulture),
                    LockoutEnabled = member.IsLockedOut,
                    LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                    UserName = member.Username,
                    PasswordHash = member.RawPasswordValue
                });
            }
            throw new InvalidOperationException("The user id must be an integer");
        }

        public Task<T> FindByNameAsync(string userName)
        {
            var member = ApplicationContext.Current.Services.MemberService.GetByUsername(userName);
            if (member == null)
            {
                return Task.FromResult<T>(null);
            }

            return Task.FromResult(new T
            {
                Email = member.Email,
                Id = member.Id.ToString(CultureInfo.InvariantCulture),
                LockoutEnabled = member.IsLockedOut,
                LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                UserName = member.Username,
                PasswordHash = member.RawPasswordValue
            });
        }

        public void Dispose()
        {
            //nothing to dispose
        }

        public Task SetPasswordHashAsync(T user, string passwordHash)
        {
            return Task.Run(() => user.PasswordHash = passwordHash);
        }

        public Task<string> GetPasswordHashAsync(T user)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(T user)
        {
            return Task.FromResult(user.PasswordHash.IsNullOrWhiteSpace() == false);
        }
    }
}