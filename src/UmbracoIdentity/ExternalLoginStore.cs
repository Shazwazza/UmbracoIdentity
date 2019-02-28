using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Identity;
using NPoco;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.Scoping;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    /// <summary>
    /// We are using a custom sqlce db to store this information
    /// </summary>
    public class ExternalLoginStore :  IExternalLoginStore
    {
        private static bool _dbInitialized;
        private static bool _isInitialized;
        private static object _locker = new object();


        private readonly ILogger _logger;
        private readonly IScopeProvider _scopeProvider;
        public const string TableName = "ExternalLogins";

        private Sql<ISqlContext> Sql() => _scopeProvider.SqlContext.Sql();

        /// <summary>
        /// Constructor which can be used to define a SQLCE based login store
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="scopeProvider">
        /// The Umbraco database context
        /// </param>
        public ExternalLoginStore(ILogger logger, IScopeProvider scopeProvider)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (scopeProvider == null) throw new ArgumentNullException(nameof(scopeProvider));

            _logger = logger;
            _scopeProvider = scopeProvider;
        }

        private void EnsureInitialized()
        {
            LazyInitializer.EnsureInitialized(ref _dbInitialized, ref _isInitialized, ref _locker, () =>
            {
                EnsureTable();
                return true;
            });
        }

        /// <summary>
        /// Checks if the table exists and if not it creates it
        /// </summary>
        private void EnsureTable()
        {      
            // TODO: Replace with Migration
            //var db = GetDatabase();
            //var schemaHelper = new DatabaseSchemaHelper(db, _logger, _sqlSyntaxProvider);
            //if (!schemaHelper.TableExist(TableName))
            //{
            //    using (var transaction = db.GetTransaction())
            //    {
            //        schemaHelper.CreateTable<ExternalLoginDto>();
            //        transaction.Complete();
            //    }
            //    _logger.Info<ExternalLoginStore>(string.Format("New table '{0}' was created", TableName));
            //}
        }
        
        public IEnumerable<IdentityMemberLogin<int>> GetAll(int userId)
        {
            EnsureInitialized();

            IEnumerable<IdentityMemberLogin<int>> response;
            using (var scope = _scopeProvider.CreateScope())
            {
                var sql = Sql()
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
            EnsureInitialized();

            IEnumerable<int> response;
            using (var scope = _scopeProvider.CreateScope())
            {
                var sql = Sql()
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
            EnsureInitialized();

            using (var scope = _scopeProvider.CreateScope())
            {
                //clear out logins for member
                scope.Database.Execute("DELETE FROM ExternalLogins WHERE UserId=@userId", new { userId = memberId });

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
            EnsureInitialized();

            using (var scope = _scopeProvider.CreateScope())
            {
                scope.Database.Execute("DELETE FROM ExternalLogins WHERE UserId=@userId", new { userId = memberId });
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
