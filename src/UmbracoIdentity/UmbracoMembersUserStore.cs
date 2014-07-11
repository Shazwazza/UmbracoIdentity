using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Services;
using Task = System.Threading.Tasks.Task;

namespace UmbracoIdentity
{
    /// <summary>
    /// A custom user store that uses Umbraco member data
    /// </summary>
    public class UmbracoMembersUserStore<T> : DisposableObject, IUserStore<T, int>, IUserPasswordStore<T, int>, IUserEmailStore<T, int>, IUserLoginStore<T, int> 
        where T : UmbracoIdentityUser, IUser<int>, new()
    {
        private readonly IMemberService _memberService;
        private readonly IdentityEnabledMembersMembershipProvider _membershipProvider;
        private readonly ExternalLoginStore _externalLoginStore = new ExternalLoginStore();

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
            if (user == null) throw new ArgumentNullException("user");

            return Task.Run(() =>
            {
                var member = _memberService.CreateMember(
                    user.UserName, user.Email, 
                    user.Name.IsNullOrWhiteSpace() ? user.UserName : user.Name,
                    _membershipProvider.DefaultMemberTypeAlias);
                UpdateMemberProperties(member, user);

                //the password must be 'something' it could be empty if authenticating
                // with an external provider so we'll just guid it
                if (member.RawPasswordValue.IsNullOrWhiteSpace())
                {
                    member.RawPasswordValue = Guid.NewGuid().ToString("N");
                }
                _memberService.Save(member);    

                //re-assign id
                user.Id = member.Id;
            });
        }

        public virtual async Task UpdateAsync(T user)
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

                if (user.Logins.Any())
                {
                    var logins = await GetLoginsAsync(user);
                    _externalLoginStore.SaveUserLogins(found.Id, logins);
                }
            }           
        }

        public Task DeleteAsync(T user)
        {
            if (user == null) throw new ArgumentNullException("user");

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

        public Task<T> FindByIdAsync(int userId)
        {
            return Task.Run(() =>
            {
                var member = _memberService.GetById(userId);
                if (member == null)
                {
                    return null;
                }
                return new T
                {
                    Email = member.Email,
                    Id = member.Id,
                    LockoutEnabled = member.IsLockedOut,
                    LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                    UserName = member.Username,
                    PasswordHash = member.RawPasswordValue
                };
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
                    Id = member.Id,
                    LockoutEnabled = member.IsLockedOut,
                    LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                    UserName = member.Username,
                    PasswordHash = member.RawPasswordValue
                };
            });
        }

        public Task SetPasswordHashAsync(T user, string passwordHash)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (passwordHash.IsNullOrWhiteSpace()) throw new ArgumentNullException("passwordHash");

            return Task.Run(() => user.PasswordHash = passwordHash);
        }

        public Task<string> GetPasswordHashAsync(T user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(T user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.PasswordHash.IsNullOrWhiteSpace() == false);
        }


        public Task SetEmailAsync(T user, string email)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (email.IsNullOrWhiteSpace()) throw new ArgumentNullException("email");

            return Task.Run(() => user.Email = email);
        }

        public Task<string> GetEmailAsync(T user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(T user)
        {
            if (user == null) throw new ArgumentNullException("user");

            throw new NotImplementedException();
        }

        public Task SetEmailConfirmedAsync(T user, bool confirmed)
        {
            if (user == null) throw new ArgumentNullException("user");

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
                        Id = member.Id,
                        LockoutEnabled = member.IsLockedOut,
                        LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                        UserName = member.Username,
                        PasswordHash = member.RawPasswordValue
                    };
            });
        }

        public Task AddLoginAsync(T user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            return Task.Run(() =>
            {
                var logins = user.Logins;
                var instance = new IdentityUserLogin<int>
                {
                    UserId = user.Id,
                    ProviderKey = login.ProviderKey,
                    LoginProvider = login.LoginProvider
                };
                var userLogin = instance;
                logins.Add(userLogin);
            });
        }

        public Task RemoveLoginAsync(T user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            return Task.Run(() =>
            {
                var provider = login.LoginProvider;
                var key = login.ProviderKey;
                var userLogin = user.Logins.SingleOrDefault((l => l.LoginProvider == provider && l.ProviderKey == key));
                if (userLogin != null)
                    user.Logins.Remove(userLogin);
            });
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(T user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.Run(() => (IList<UserLoginInfo>) 
                user.Logins.Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey)).ToList());
        }

        public Task<T> FindAsync(UserLoginInfo login)
        {
            return Task.Run(() =>
            {
                var result = _externalLoginStore.Find(login);
                if (result != null)
                {
                    var member = _memberService.GetById(result.Value);

                    return new T
                    {
                        Email = member.Email,
                        Id = member.Id,
                        LockoutEnabled = member.IsLockedOut,
                        LockoutEndDateUtc = DateTime.MaxValue.ToUniversalTime(),
                        UserName = member.Username,
                        PasswordHash = member.RawPasswordValue
                    };
                }

                return null;
            });
        }
        
        protected override void DisposeResources()
        {
            _externalLoginStore.Dispose();
        }

        private bool UpdateMemberProperties(IMember member, T user)
        {
            var anythingChanged = false;
            //don't assign anything if nothing has changed as this will trigger
            //the track changes of the model
            if (member.Name != user.Name)
            {
                anythingChanged = true;
                member.Name = user.Name;
            }
            if (member.Email != user.Email)
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
            if (member.Username != user.UserName)
            {
                anythingChanged = true;
                member.Username = user.UserName;
            }
            if (member.RawPasswordValue != user.PasswordHash)
            {
                anythingChanged = true;
                member.RawPasswordValue = user.PasswordHash;
            }
            return anythingChanged;
        }
    }

    /// <summary>
    /// Currently we're storing external logins in files in App_Data/TEMP/UmbracoIdentity - 
    /// TODO: this should be handled in the db, we'll end up with a ton of files if there's lots of members!
    /// </summary>
    internal class ExternalLoginStore : DisposableObject
    {
        //TODO: What is the OWIN form of MapPath??

        public static readonly object Locker = new object();
        private readonly UmbracoDatabase _db;

        private const string ConnString = @"Data Source=|DataDirectory|\UmbracoIdentity.sdf;Flush Interval=1;";

        public ExternalLoginStore()
        {
            if (!System.IO.File.Exists(IOHelper.MapPath("~/App_Data/UmbracoIdentity.sdf")))
            {
                using (var en = new SqlCeEngine(ConnString))
                {
                    en.CreateDatabase();
                }    
            }
            _db = new UmbracoDatabase(ConnString, "System.Data.SqlServerCe.4.0");
            if (!_db.TableExist("ExternalIdentities"))
            {
                _db.CreateTable<ExternalLoginDto>();
            }
        }

        public int? Find(UserLoginInfo login)
        {
            var sql = new Sql() 
                .Select("*")
                .From<ExternalLoginDto>()
                .Where<ExternalLoginDto>(dto => dto.LoginProvider == login.LoginProvider && dto.ProviderKey == login.ProviderKey);

            var found = _db.Fetch<ExternalLoginDto>(sql);

            return found.Any()
                ? found.First().UserId
                : (int?) null;
        }

        public void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins)
        {
            using (var t = _db.GetTransaction())
            {
                //clear out logins for member
                _db.Execute("DELETE FROM ExternalLogins WHERE UserId=@userId", new {userId = memberId});

                //add them all
                foreach (var l in logins)
                {
                    _db.Insert(new ExternalLoginDto
                    {
                        LoginProvider = l.LoginProvider,
                        ProviderKey = l.ProviderKey,
                        UserId = memberId
                    });
                }

                t.Complete();
            }
        }

        protected override void DisposeResources()
        {
            _db.Dispose();
        }

        [TableName("ExternalLogins")]
        [ExplicitColumns]
        [PrimaryKey("ExternalLoginId")]
        internal class ExternalLoginDto
        {
            [Column("ExternalLoginId")]
            [PrimaryKeyColumn(Name = "PK_ExternalLoginId")]
            public int ExternalLoginId { get; set; }

            [Column("UserId")]
            public int UserId { get; set; }

            [Column("LoginProvider")]
            [Length(4000)]
            [NullSetting(NullSetting = NullSettings.NotNull)]
            public string LoginProvider { get; set; }

            [Column("ProviderKey")]
            [Length(4000)]
            [NullSetting(NullSetting = NullSettings.NotNull)]
            public string ProviderKey { get; set; }
        }
    }
}