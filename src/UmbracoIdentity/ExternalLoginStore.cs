using Microsoft.AspNet.Identity;
using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Scoping;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    /// <summary>
    /// We are using a custom sqlce db to store this information
    /// </summary>
    public class ExternalLoginStore :  IExternalLoginStore
    {
        private readonly IScopeProvider _scopeProvider;
        public const string TableName = "ExternalLogins";

        public ExternalLoginStore(IScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        }

        public IEnumerable<IdentityMemberLogin<int>> GetAll(int userId)
        {
            IEnumerable<IdentityMemberLogin<int>> response;
            using (var scope = _scopeProvider.CreateScope())
            {
                var sql = scope.SqlContext.Sql()
                    .Select<ExternalLoginDto>()
                    .From<ExternalLoginDto>()
                    .Where<ExternalLoginDto>(x => x.UserId == userId);

                var found = scope.Database.Fetch<ExternalLoginDto>(sql);

                response = found.Select(x => new IdentityMemberLogin<int>
                {
                    LoginProvider = x.LoginProvider,
                    ProviderKey = x.ProviderKey,
                    UserId = x.UserId
                });
                scope.Complete();
            }

            return response;
        }

        public IEnumerable<int> Find(UserLoginInfo login)
        {
            IEnumerable<int> response;
            using (var scope = _scopeProvider.CreateScope())
            {
                var sql = scope.SqlContext.Sql()
                    .Select<ExternalLoginDto>()
                    .From<ExternalLoginDto>()
                    .Where<ExternalLoginDto>(x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey);

                var found = scope.Database.Fetch<ExternalLoginDto>(sql);

                response = found.Select(x => x.UserId);
                scope.Complete();
            }

            return response;
        }

        public void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                //clear out logins for member
                scope.Database.Execute($"DELETE FROM {TableName} WHERE UserId=@userId", new { userId = memberId });

                //add them all
                foreach (var l in logins)
                {
                    scope.Database.Insert(new ExternalLoginDto
                    {
                        LoginProvider = l.LoginProvider,
                        ProviderKey = l.ProviderKey,
                        UserId = memberId
                    });
                }

                scope.Complete();
            }
        }

        public void DeleteUserLogins(int memberId)
        {
            using (var scope = _scopeProvider.CreateScope())
            {
                scope.Database.Execute($"DELETE FROM {TableName} WHERE UserId=@userId", new { userId = memberId });
                scope.Complete();
            }
        }

        [TableName(TableName)]
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
