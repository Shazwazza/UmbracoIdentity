using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.AspNet.Identity;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Composing;
using Umbraco.Web.Models;
using UmbracoIdentity.Models;
using Task = System.Threading.Tasks.Task;

namespace UmbracoIdentity
{

    /// <summary>
    /// A custom user store that uses Umbraco member data
    /// </summary>
    public class UmbracoMembersUserStore<TMember> : DisposableObjectSlim, 
        IUserStore<TMember, int>, 
        IUserPasswordStore<TMember, int>, 
        IUserEmailStore<TMember, int>, 
        IUserLoginStore<TMember, int>, 
        IUserRoleStore<TMember, int>,
        IUserSecurityStampStore<TMember, int> 
        where TMember : UmbracoIdentityMember, IUser<int>, new()
    {
        private readonly ILogger _logger;
        private readonly IMemberService _memberService;
        private readonly IMemberTypeService _memberTypeService;
        private readonly IMemberGroupService _memberGroupService;
        private readonly IdentityEnabledMembersMembershipProvider _membershipProvider;
        private readonly IExternalLoginStore _externalLoginStore;
        private bool _disposed = false;

        private const string _emptyPasswordPrefix = "___UIDEMPTYPWORD__";

        public UmbracoMembersUserStore(
            ILogger logger,
            IMemberService memberService,
            IMemberTypeService memberTypeService,
            IMemberGroupService memberGroupService,
            IdentityEnabledMembersMembershipProvider membershipProvider,
            IExternalLoginStore externalLoginStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memberService = memberService ?? throw new ArgumentNullException("memberService");
            _memberTypeService = memberTypeService;
            _memberGroupService = memberGroupService ?? throw new ArgumentNullException(nameof(memberGroupService));
            _membershipProvider = membershipProvider ?? throw new ArgumentNullException("membershipProvider");
            _externalLoginStore = externalLoginStore ?? throw new ArgumentNullException("externalLoginStore");

            if (_membershipProvider.PasswordFormat != MembershipPasswordFormat.Hashed)
            {
                throw new InvalidOperationException("Cannot use ASP.Net Identity with UmbracoMembersUserStore when the password format is not Hashed");
            }
        }

        public virtual async Task CreateAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");
            
            var member = _memberService.CreateMember(
                    user.UserName, user.Email,
                    user.Name.IsNullOrWhiteSpace() ? user.UserName : user.Name,
                    user.MemberTypeAlias.IsNullOrWhiteSpace() ? _membershipProvider.DefaultMemberTypeAlias: user.MemberTypeAlias);

            UpdateMemberProperties(member, user);

            //the password must be 'something' it could be empty if authenticating
            // with an external provider so we'll just generate one and prefix it, the 
            // prefix will help us determine if the password hasn't actually been specified yet.
            if (member.RawPasswordValue.IsNullOrWhiteSpace())
            {
                //this will hash the guid with a salt so should be nicely random
                var aspHasher = new PasswordHasher();
                member.RawPasswordValue = _emptyPasswordPrefix +
                    aspHasher.HashPassword(Guid.NewGuid().ToString("N"));

            }
            _memberService.Save(member);

            //re-assign id
            user.Id = member.Id;

            if (user.LoginsChanged)
            {
                var logins = await GetLoginsAsync(user);
                _externalLoginStore.SaveUserLogins(member.Id, logins);
            }

            if (user.RolesChanged)
            {
                IMembershipRoleService<IMember> memberRoleService = _memberService;

                var persistedRoles = memberRoleService.GetAllRoles(member.Id).ToArray();
                var userRoles = user.Roles.Select(x => x.RoleName).ToArray();

                var keep = persistedRoles.Intersect(userRoles).ToArray();
                var remove = persistedRoles.Except(keep).ToArray();
                var add = userRoles.Except(persistedRoles).ToArray();

                memberRoleService.DissociateRoles(new[] { member.Id }, remove);
                memberRoleService.AssignRoles(new[] { member.Id }, add);
            }
            
        }

        /// <summary>
        /// Performs the persistence for the member
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public virtual async Task UpdateAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            var asInt = user.Id.TryConvertTo<int>();
            if (!asInt)
            {
                throw new InvalidOperationException("The user id must be an integer to work with the UmbracoMembersUserStore");
            }

            var found = _memberService.GetById(asInt.Result);
            if (found != null)
            {
                if (UpdateMemberProperties(found, user))
                {
                    _memberService.Save(found);
                }

                if (user.LoginsChanged)
                {
                    var logins = await GetLoginsAsync(user);
                    _externalLoginStore.SaveUserLogins(found.Id, logins);
                }

                if (user.RolesChanged)
                {
                    IMembershipRoleService<IMember> memberRoleService = _memberService;

                    var persistedRoles = memberRoleService.GetAllRoles(found.Id).ToArray();
                    var userRoles = user.Roles.Select(x => x.RoleName).ToArray();

                    var keep = persistedRoles.Intersect(userRoles).ToArray();
                    var remove = persistedRoles.Except(keep).ToArray();
                    var add = userRoles.Except(persistedRoles).ToArray();

                    if (remove.Any())
                        memberRoleService.DissociateRoles(new[] {found.Id}, remove);
                    if (add.Any())
                        memberRoleService.AssignRoles(new[] {found.Id}, add);
                }
            }           
        }

        public Task DeleteAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            var asInt = user.Id.TryConvertTo<int>();
            if (!asInt)
            {
                throw new InvalidOperationException("The user id must be an integer to work with the UmbracoMembersUserStore");
            }

            var found = _memberService.GetById(asInt.Result);
            if (found != null)
            {
                _memberService.Delete(found);
            }
            _externalLoginStore.DeleteUserLogins(asInt.Result);

            return Task.FromResult(0);
        }

        public Task<TMember> FindByIdAsync(int userId)
        {
            ThrowIfDisposed();
            var member = _memberService.GetById(userId);
            if (member == null)
            {
                return Task.FromResult((TMember) null);
            }

            var result = AssignUserDataCallback(MapFromMember(member));

            return Task.FromResult(result);
        }

        public Task<TMember> FindByNameAsync(string userName)
        {
            ThrowIfDisposed();
            var member = _memberService.GetByUsername(userName);
            if (member == null)
            {
                return Task.FromResult((TMember)null);
            }

            var result = AssignUserDataCallback(MapFromMember(member));

            return Task.FromResult(result);
        }

        public Task SetPasswordHashAsync(TMember user, string passwordHash)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");
            if (passwordHash.IsNullOrWhiteSpace()) throw new ArgumentNullException("passwordHash");

            user.PasswordHash = passwordHash;

            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.PasswordHash.IsNullOrWhiteSpace() == false);
        }


        public Task SetEmailAsync(TMember user, string email)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");
            if (email.IsNullOrWhiteSpace()) throw new ArgumentNullException("email");

            user.Email = email;

            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            throw new NotImplementedException();
        }

        public Task SetEmailConfirmedAsync(TMember user, bool confirmed)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            throw new NotImplementedException();
        }

        public Task<TMember> FindByEmailAsync(string email)
        {
            ThrowIfDisposed();
            var member = _memberService.GetByEmail(email);
            var result = member == null
                ? null
                : MapFromMember(member);

            var r = AssignUserDataCallback(result);

            return Task.FromResult(r);
        }

        public Task AddLoginAsync(TMember user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            var logins = user.Logins;
            var instance = new IdentityMemberLogin<int>
            {
                UserId = user.Id,
                ProviderKey = login.ProviderKey,
                LoginProvider = login.LoginProvider
            };
            var userLogin = instance;
            logins.Add(userLogin);

            return Task.FromResult(0);
        }

        public Task RemoveLoginAsync(TMember user, UserLoginInfo login)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            var provider = login.LoginProvider;
            var key = login.ProviderKey;
            var userLogin = user.Logins.SingleOrDefault((l => l.LoginProvider == provider && l.ProviderKey == key));
            if (userLogin != null)
                user.Logins.Remove(userLogin);

            return Task.FromResult(0);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            var result = (IList<UserLoginInfo>)
                user.Logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey)).ToList();

            return Task.FromResult(result);
        }

        public Task<TMember> FindAsync(UserLoginInfo login)
        {
            ThrowIfDisposed();
            //get all logins associated with the login id
            var result = _externalLoginStore.Find(login).ToArray();
            if (result.Any())
            {
                //return the first member that matches the result
                var user = (from id in result
                    select _memberService.GetById(id)
                    into member
                    where member != null
                    select MapFromMember(member)).FirstOrDefault();

                return Task.FromResult(AssignUserDataCallback(user));
            }

            return Task.FromResult((TMember)null);
        }


        /// <summary>
        /// Adds a user to a role
        /// </summary>
        /// <param name="user"/><param name="roleName"/>
        /// <returns/>
        public Task AddToRoleAsync(TMember user, string roleName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("roleName is null or empty");
            }

            var roleEntity = _memberGroupService.GetByName(roleName);
            if (roleEntity == null)
            {
                throw new InvalidOperationException(roleName + " does not exist as a role");
            }

            var roles = user.Roles;
            var instance = new IdentityMemberRole { UserId = user.Id, RoleName = roleEntity.Name };
            var userRole = instance;
            roles.Add(userRole);

            return Task.FromResult(0);            
        }

        /// <summary>
        /// Removes the role for the user
        /// </summary>
        /// <param name="user"/><param name="roleName"/>
        /// <returns/>
        public Task RemoveFromRoleAsync(TMember user, string roleName)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (string.IsNullOrWhiteSpace(roleName))
            {
                throw new ArgumentException("roleName is null or empty");
            }

            var memberRole = user.Roles.SingleOrDefault(l => l.RoleName.InvariantEquals(roleName));
            if (memberRole != null)
                user.Roles.Remove(memberRole);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns the roles for this user
        /// </summary>
        /// <param name="user"/>
        /// <returns/>
        public Task<IList<string>> GetRolesAsync(TMember user)
        {
            ThrowIfDisposed();
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult((IList<string>) new List<string>(user.Roles.Select(l => l.RoleName)));
        }

        /// <summary>
        /// Returns true if a user is in the role
        /// </summary>
        /// <param name="user"/><param name="roleName"/>
        /// <returns/>
        public Task<bool> IsInRoleAsync(TMember user, string roleName)
        {
            ThrowIfDisposed();
            return Task.FromResult(user.Roles.Any(x => x.RoleName.InvariantEquals(roleName)));
        }

        public Task SetSecurityStampAsync(TMember user, string stamp)
        {
            ThrowIfDisposed();

            if (user == null) throw new ArgumentNullException(nameof(user));

            user.SecurityStamp = stamp;
            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(TMember user)
        {
            ThrowIfDisposed();

            if (user == null) throw new ArgumentNullException(nameof(user));

            //the stamp cannot be null, so if it is currently null then we'll just return a hash of the password
            return Task.FromResult(user.SecurityStamp.IsNullOrWhiteSpace()
                //ensure that the stored password is not empty either! it shouldn't be but just in case we'll return a new guid
                ? (user.StoredPassword.IsNullOrWhiteSpace() ? Guid.NewGuid().ToString() : user.StoredPassword.GenerateHash())
                : user.SecurityStamp);
        }

        protected override void DisposeResources()
        {
            _disposed = true;
        }

        private TMember MapFromMember(IMember member)
        {
            var result = new TMember
            {
                Email = member.Email,
                Id = member.Id,
                LockoutEnabled = member.IsLockedOut,
                LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                UserName = member.Username,
                PasswordHash = GetPasswordHash(member.RawPasswordValue),
                StoredPassword = member.RawPasswordValue,
                Name = member.Name,
                MemberTypeAlias = member.ContentTypeAlias
            };

            bool propertyTypeExists;
            var memberSecurityStamp = member.GetSecurityStamp(out propertyTypeExists);
            if (!propertyTypeExists)
            {
                _logger.Warn<UmbracoMembersUserStore<TMember>>($"The {UmbracoIdentityConstants.SecurityStampProperty} does not exist on the member type {member.ContentType.Alias}, see docs on how to fix: https://github.com/Shazwazza/UmbracoIdentity/wiki");
            }
            else
            {
                result.SecurityStamp = memberSecurityStamp;
            }

            result.MemberProperties = GetMemberProperties(member).ToList();

            return result;
        }

        private IEnumerable<UmbracoProperty> GetMemberProperties(IMember member)
        {
            var builtIns = Umbraco.Core.Constants.Conventions.Member.GetStandardPropertyTypeStubs()
                .Select(x => x.Key).ToArray();

            var memberType = _memberTypeService.Get(member.ContentTypeId);

            var viewProperties = new List<UmbracoProperty>();

            foreach (var prop in memberType.PropertyTypes.Where(x => builtIns.Contains(x.Alias) == false))
            {
                var value = string.Empty;

                var propValue = member.Properties[prop.Alias];
                if (propValue != null && propValue.GetValue() != null)
                {
                    value = propValue.GetValue().ToString();
                }

                var viewProperty = new UmbracoProperty
                {
                    Alias = prop.Alias,
                    Name = prop.Name,
                    Value = value
                };

                viewProperties.Add(viewProperty);
            }
            return viewProperties;
        }


        private bool UpdateMemberProperties(IMember member, TMember user)
        {
            var anythingChanged = false;
            //don't assign anything if nothing has changed as this will trigger
            //the track changes of the model
            if (member.Name != user.Name && user.Name.IsNullOrWhiteSpace() == false)
            {
                anythingChanged = true;
                member.Name = user.Name;
            }
            if (member.Email != user.Email && user.Email.IsNullOrWhiteSpace() == false)
            {
                anythingChanged = true;
                member.Email = user.Email;
            }
            if (member.FailedPasswordAttempts != user.AccessFailedCount)
            {
                anythingChanged = true;
                member.FailedPasswordAttempts = user.AccessFailedCount;
            }
            if (member.IsLockedOut != user.LockoutEnabled)
            {
                anythingChanged = true;
                member.IsLockedOut = user.LockoutEnabled;
            }
            if (member.Username != user.UserName && user.UserName.IsNullOrWhiteSpace() == false)
            {
                anythingChanged = true;
                member.Username = user.UserName;
            }
            if (member.RawPasswordValue != user.PasswordHash && user.PasswordHash.IsNullOrWhiteSpace() == false)
            {
                anythingChanged = true;
                member.RawPasswordValue = user.PasswordHash;
            }
            bool propertyTypeExists;
            var memberSecurityStamp = member.GetSecurityStamp(out propertyTypeExists);
            if (memberSecurityStamp != null && memberSecurityStamp != user.SecurityStamp)
            {
                anythingChanged = true;
                member.Properties[UmbracoIdentityConstants.SecurityStampProperty].SetValue(user.SecurityStamp);
            }

            if (user.MemberProperties != null)
            {
                var memberType = _memberTypeService.Get(member.ContentTypeId);

                foreach (var property in user.MemberProperties
                    //ensure the property they are posting exists
                    .Where(p => memberType.PropertyTypeExists(p.Alias))
                    .Where(p => member.Properties.Contains(p.Alias))
                    //needs to be editable
                    .Where(p => memberType.MemberCanEditProperty(p.Alias))
                    //needs to be different
                    .Where(p =>
                    {
                        var mPropAsString = member.Properties[p.Alias].GetValue() == null ? string.Empty : member.Properties[p.Alias].GetValue().ToString();
                        var uPropAsString = p.Value ?? string.Empty;
                        return mPropAsString != uPropAsString;
                    }))
                {
                    anythingChanged = true;
                    member.Properties[property.Alias].SetValue(property.Value);
                }
            }
            
            return anythingChanged;
        }

        

        /// <summary>
        /// This checks if the password 
        /// </summary>
        /// <param name="storedPass"></param>
        /// <returns></returns>
        private string GetPasswordHash(string storedPass)
        {
            return storedPass.StartsWith(_emptyPasswordPrefix) ? null : storedPass;
        }

        /// <summary>
        /// This is used to assign the lazy callbacks for the user's data collections
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private TMember AssignUserDataCallback(TMember user)
        {
            if (user != null)
            {
                user.SetLoginsCallback(new Lazy<IEnumerable<IdentityMemberLogin<int>>>(() =>
                            _externalLoginStore.GetAll(user.Id)));

                user.SetRolesCallback(new Lazy<IEnumerable<IdentityMemberRole<int>>>(() =>
                {
                    IMembershipRoleService<IMember> memberRoleService = _memberService;
                    var roles = memberRoleService.GetAllRoles(user.Id);
                    return roles.Select(x => new IdentityMemberRole()
                    {
                        RoleName = x,
                        UserId = user.Id
                    });
                }));
            }
            return user;
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
