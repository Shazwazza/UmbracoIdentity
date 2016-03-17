using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.AspNet.Identity;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Models;
using UmbracoIdentity.Models;
using Task = System.Threading.Tasks.Task;

namespace UmbracoIdentity
{


    /// <summary>
    /// A custom user store that uses Umbraco member data
    /// </summary>
    public class UmbracoMembersUserStore<TMember> : DisposableObject, 
        IUserStore<TMember, int>, 
        IUserPasswordStore<TMember, int>, 
        IUserEmailStore<TMember, int>, 
        IUserLoginStore<TMember, int>, 
        IUserRoleStore<TMember, int>        
        where TMember : UmbracoIdentityMember, IUser<int>, new()
    {
        private readonly IMemberService _memberService;
        private readonly IMemberTypeService _memberTypeService;
        private readonly IMemberGroupService _memberGroupService;
        private readonly IdentityEnabledMembersMembershipProvider _membershipProvider;
        private readonly IExternalLoginStore _externalLoginStore;

        public UmbracoMembersUserStore(
            IMemberService memberService, 
            IMemberTypeService memberTypeService,
            IMemberGroupService memberGroupService,
            IdentityEnabledMembersMembershipProvider membershipProvider, 
            IExternalLoginStore externalLoginStore)
        {
            if (memberService == null) throw new ArgumentNullException("memberService");
            if (memberGroupService == null) throw new ArgumentNullException(nameof(memberGroupService));
            if (membershipProvider == null) throw new ArgumentNullException("membershipProvider");
            if (externalLoginStore == null) throw new ArgumentNullException("externalLoginStore");

            _memberService = memberService;
            _memberTypeService = memberTypeService;
            _memberGroupService = memberGroupService;
            _membershipProvider = membershipProvider;
            _externalLoginStore = externalLoginStore;

            if (_membershipProvider.PasswordFormat != MembershipPasswordFormat.Hashed)
            {
                throw new InvalidOperationException("Cannot use ASP.Net Identity with UmbracoMembersUserStore when the password format is not Hashed");
            }
        }

        public virtual async Task CreateAsync(TMember user)
        {
            if (user == null) throw new ArgumentNullException("user");

            var member = _memberService.CreateMember(
                    user.UserName, user.Email,
                    user.Name.IsNullOrWhiteSpace() ? user.UserName : user.Name,
                    _membershipProvider.DefaultMemberTypeAlias);
            UpdateMemberProperties(member, user);

            //the password must be 'something' it could be empty if authenticating
            // with an external provider so we'll just generate one and prefix it, the 
            // prefix will help us determine if the password hasn't actually been specified yet.
            if (member.RawPasswordValue.IsNullOrWhiteSpace())
            {
                //this will hash the guid with a salt so should be nicely random
                var aspHasher = new PasswordHasher();
                member.RawPasswordValue = "___UIDEMPTYPWORD__" +
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

                    memberRoleService.DissociateRoles(new[] {found.Id}, remove);
                    memberRoleService.AssignRoles(new[] {found.Id}, add);
                }
            }           
        }

        public Task DeleteAsync(TMember user)
        {
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
            if (user == null) throw new ArgumentNullException("user");
            if (passwordHash.IsNullOrWhiteSpace()) throw new ArgumentNullException("passwordHash");

            user.PasswordHash = passwordHash;

            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TMember user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TMember user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.PasswordHash.IsNullOrWhiteSpace() == false);
        }


        public Task SetEmailAsync(TMember user, string email)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (email.IsNullOrWhiteSpace()) throw new ArgumentNullException("email");

            user.Email = email;

            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(TMember user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TMember user)
        {
            if (user == null) throw new ArgumentNullException("user");

            throw new NotImplementedException();
        }

        public Task SetEmailConfirmedAsync(TMember user, bool confirmed)
        {
            if (user == null) throw new ArgumentNullException("user");

            throw new NotImplementedException();
        }

        public Task<TMember> FindByEmailAsync(string email)
        {
            var member = _memberService.GetByEmail(email);
            var result = member == null
                ? null
                : MapFromMember(member);

            var r = AssignUserDataCallback(result);

            return Task.FromResult(r);
        }

        public Task AddLoginAsync(TMember user, UserLoginInfo login)
        {
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
            if (user == null) throw new ArgumentNullException("user");

            var result = (IList<UserLoginInfo>)
                user.Logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey)).ToList();

            return Task.FromResult(result);
        }

        public Task<TMember> FindAsync(UserLoginInfo login)
        {
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
            return Task.FromResult(user.Roles.Any(x => x.RoleName.InvariantEquals(roleName)));
        }

        protected override void DisposeResources()
        {
            _externalLoginStore.Dispose();
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
                Name = member.Name
            };

            result.MemberProperties = GetMemberProperties(member).ToList();

            return result;
        }

        private IEnumerable<UmbracoProperty> GetMemberProperties(IMember member)
        {
            var builtIns = ((Dictionary<string, PropertyType>)
                typeof (Umbraco.Core.Constants.Conventions.Member).CallStaticMethod("GetStandardPropertyTypeStubs"))
                .Select(x => x.Key).ToArray();

            var memberType = _memberTypeService.Get(member.ContentTypeAlias);
            if (memberType == null) throw new NullReferenceException("No member type found with alias " + member.ContentTypeAlias);

            var viewProperties = new List<UmbracoProperty>();

            foreach (var prop in memberType.PropertyTypes.Where(x => builtIns.Contains(x.Alias) == false))
            {
                var value = string.Empty;

                var propValue = member.Properties[prop.Alias];
                if (propValue != null && propValue.Value != null)
                {
                    value = propValue.Value.ToString();
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

            if (user.MemberProperties != null)
            {
                foreach (var property in user.MemberProperties
                    //ensure the property they are posting exists
                    .Where(p => member.ContentType.PropertyTypeExists(p.Alias))
                    .Where(p => member.Properties.Contains(p.Alias))
                    //needs to be editable
                    .Where(p => member.ContentType.MemberCanEditProperty(p.Alias))
                    //needs to be different
                    .Where(p =>
                    {
                        var mPropAsString = member.Properties[p.Alias].Value == null ? string.Empty : member.Properties[p.Alias].Value.ToString();
                        var uPropAsString = p.Value ?? string.Empty;
                        return mPropAsString != uPropAsString;
                    }))
                {
                    anythingChanged = true;
                    member.Properties[property.Alias].Value = property.Value;
                }
            }
            
            return anythingChanged;
        }

        private string GetPasswordHash(string storedPass)
        {
            return storedPass.StartsWith("___UIDEMPTYPWORD__") ? null : storedPass;
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

    }
}