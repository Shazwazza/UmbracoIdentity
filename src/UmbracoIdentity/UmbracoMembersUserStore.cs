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
    public class UmbracoMembersUserStore<T> : IUserStore<T>, IUserPasswordStore<T>, IUserEmailStore<T>
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

        public virtual Task CreateAsync(T user)
        {
            return Task.Run(() =>
            {
                _memberService.CreateMemberWithIdentity(
                    user.UserName, user.Email, user.UserName,
                    _membershipProvider.DefaultMemberTypeAlias);
            });
        }

        public virtual Task UpdateAsync(T user)
        {
            return Task.Run(() =>
            {
                var asInt = user.Id.TryConvertTo<int>();
                if (!asInt)
                {
                    throw new InvalidOperationException("The user id must be an integer to work with the UmbracoMembersUserStore");
                }

                var found = _memberService.GetById(asInt.Result);
                if (found != null)
                {
                    var anythingChanged = false;
                    //don't assign anything if nothing has changed as this will trigger
                    //the track changes of the model
                    if (found.Email != user.Email)
                    {
                        anythingChanged = true;
                        found.Email = user.Email;    
                    }
                    if (found.FailedPasswordAttempts != user.AccessFailedCount)
                    {
                        anythingChanged = true;
                        found.FailedPasswordAttempts = user.AccessFailedCount;    
                    }
                    if (found.IsLockedOut != user.LockoutEnabled)
                    {
                        anythingChanged = true;
                        found.IsLockedOut = user.LockoutEnabled;    
                    }
                    if (found.Username != user.UserName)
                    {
                        anythingChanged = true;
                        found.Username = user.UserName;
                    }
                    if (found.RawPasswordValue != user.PasswordHash)
                    {
                        anythingChanged = true;
                        found.RawPasswordValue = user.PasswordHash;
                    }
                    if (anythingChanged)
                    {
                        _memberService.Save(found);    
                    }
                }                
            });
        }

        public Task DeleteAsync(T user)
        {
            return Task.Run(() =>
            {
                var asInt = user.Id.TryConvertTo<int>();
                if (!asInt)
                {
                    throw new InvalidOperationException("The user id must be an integer to work with the UmbracoMembersUserStore");
                }

                var found = _memberService.GetById(asInt.Result);
                _memberService.Delete(found);
            });
        }

        public Task<T> FindByIdAsync(string userId)
        {
            return Task.Run(() =>
            {
                var attempt = userId.TryConvertTo<int>();
                if (attempt)
                {
                    var member = _memberService.GetById(attempt.Result);
                    if (member == null)
                    {
                        return null;
                    }
                    return new T
                    {
                        Email = member.Email,
                        Id = member.Id.ToString(CultureInfo.InvariantCulture),
                        LockoutEnabled = member.IsLockedOut,
                        LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                        UserName = member.Username,
                        PasswordHash = member.RawPasswordValue
                    };
                }
                throw new InvalidOperationException("The user id must be an integer");
            });
        }

        public Task<T> FindByNameAsync(string userName)
        {
            return Task.Run(() =>
            {
                var member = ApplicationContext.Current.Services.MemberService.GetByUsername(userName);
                if (member == null)
                {
                    return null;
                }

                return new T
                {
                    Email = member.Email,
                    Id = member.Id.ToString(CultureInfo.InvariantCulture),
                    LockoutEnabled = member.IsLockedOut,
                    LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                    UserName = member.Username,
                    PasswordHash = member.RawPasswordValue
                };
            });
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

        public void Dispose()
        {
            //nothing to dispose
        }

        public Task SetEmailAsync(T user, string email)
        {
            return Task.Run(() => user.Email = email);
        }

        public Task<string> GetEmailAsync(T user)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(T user)
        {
            throw new NotImplementedException();
        }

        public Task SetEmailConfirmedAsync(T user, bool confirmed)
        {
            throw new NotImplementedException();
        }

        public Task<T> FindByEmailAsync(string email)
        {
            return Task.Run(() =>
            {
                var member = _memberService.GetByEmail(email);
                return member == null
                    ? null
                    : new T
                    {
                        Email = member.Email,
                        Id = member.Id.ToString(CultureInfo.InvariantCulture),
                        LockoutEnabled = member.IsLockedOut,
                        LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                        UserName = member.Username,
                        PasswordHash = member.RawPasswordValue
                    };
            });
        }
    }
}