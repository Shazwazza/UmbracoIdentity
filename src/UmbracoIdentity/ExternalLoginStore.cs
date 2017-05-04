using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Identity;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.SqlSyntax;
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
        private readonly DatabaseContext _dbContext;
        private readonly ISqlSyntaxProvider _sqlSyntaxProvider;
        public const string TableName = "ExternalLogins";

        private UmbracoDatabase GetDatabase()
        {
            return _dbContext.Database;
        }

        /// <summary>
        /// Constructor which can be used to define a SQLCE based login store
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext">
        /// The Umbraco database context
        /// </param>
        public ExternalLoginStore(ILogger logger, DatabaseContext dbContext)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

            _logger = logger;
            _dbContext = dbContext;
            _sqlSyntaxProvider = dbContext.SqlSyntax;
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
            var db = GetDatabase();
            var schemaHelper = new DatabaseSchemaHelper(db, _logger, _sqlSyntaxProvider);
            if (!schemaHelper.TableExist(TableName))
            {
                using (var transaction = db.GetTransaction())
                {
                    schemaHelper.CreateTable<ExternalLoginDto>();
                    transaction.Complete();
                }
                LogHelper.Info<ExternalLoginStore>(string.Format("New table '{0}' was created", TableName));
            }
        }
        
        public IEnumerable<IdentityMemberLogin<int>> GetAll(int userId)
        {
            EnsureInitialized();
            
            var db = GetDatabase();

            //Can't use strongly typed here due to a bug in 7.5.6 PocoToSqlExpressionVisitor ctor
            var sql = new Sql()
                .Select("*").From(TableName).Where("UserId=@userId", new { userId = userId });

            var found = db.Fetch<ExternalLoginDto>(sql);

            return found.Select(x => new IdentityMemberLogin<int>
            {
                LoginProvider = x.LoginProvider,
                ProviderKey = x.ProviderKey,
                UserId = x.UserId
            });
        }

        public IEnumerable<int> Find(UserLoginInfo login)
        {
            EnsureInitialized();

            var db = GetDatabase();

            //Can't use strongly typed here due to a bug in 7.5.6 PocoToSqlExpressionVisitor ctor
            var sql = new Sql()
                .Select("*").From(TableName).Where("LoginProvider=@loginProvider AND ProviderKey=@providerKey", new { loginProvider = login.LoginProvider, providerKey = login.ProviderKey });

            var found = db.Fetch<ExternalLoginDto>(sql);

            return found.Select(x => x.UserId);
        }

        public void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins)
        {
            EnsureInitialized();

            var db = GetDatabase();
            using (var t = db.GetTransaction())
            {
                //clear out logins for member
                db.Execute("DELETE FROM ExternalLogins WHERE UserId=@userId", new { userId = memberId });

                //add them all
                foreach (var l in logins)
                {
                    db.Insert(new ExternalLoginDto
                    {
                        LoginProvider = l.LoginProvider,
                        ProviderKey = l.ProviderKey,
                        UserId = memberId
                    });
                }

                t.Complete();
            }
        }

        public void DeleteUserLogins(int memberId)
        {
            EnsureInitialized();

            var db = GetDatabase();
            using (var t = db.GetTransaction())
            {
                db.Execute("DELETE FROM ExternalLogins WHERE UserId=@userId", new { userId = memberId });

                t.Complete();
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
