using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Threading;
using Microsoft.AspNet.Identity;

using Microsoft.AspNet.Identity.Owin;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.Services;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    /// <summary>
    /// We are using a custom sqlce db to store this information
    /// </summary>
    public class ExternalLoginStore : DisposableObject, IExternalLoginStore
    {
        private static bool _dbInitialized;
        private static bool _isInitialized;
        private static object _locker = new object();


        private readonly ILogger _logger;
        private readonly ISqlSyntaxProvider _sqlSyntaxProvider;
        private readonly bool _useSeparateDbFile;        
        private readonly UmbracoDatabase _db;
        private const string ConnString = @"Data Source=|DataDirectory|\UmbracoIdentity.sdf;Flush Interval=1;";
        public const string TableName = "ExternalLogins";

        /// <summary>
        /// Constructor for backwards compat will use an external SQLCE DB file
        /// </summary>
        [Obsolete("Use the ctor that specifies all dependencies")]
        public ExternalLoginStore() 
            : this(ApplicationContext.Current.ProfilingLogger.Logger,
                  ApplicationContext.Current.DatabaseContext, 
                  true)
        {            
        }

        /// <summary>
        /// Constructor which can be used to define a SQLCE based login store
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext">
        /// The Umbraco database context
        /// </param>
        /// <param name="useSeparateDbFile">
        /// Set to true if a separate SQLCE db will be used (UmbracoIdentity.sdf)
        /// </param>
        public ExternalLoginStore(ILogger logger, DatabaseContext dbContext, bool useSeparateDbFile)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

            _logger = logger;
            _useSeparateDbFile = useSeparateDbFile;

            if (useSeparateDbFile)
            {
                _db = new UmbracoDatabase(ConnString, "System.Data.SqlServerCe.4.0", _logger);
                _sqlSyntaxProvider = new SqlCeSyntaxProvider();
            }
            else
            {
                _db = dbContext.Database;
                _sqlSyntaxProvider = dbContext.SqlSyntax;
            }
        }

        /// <summary>
        /// Default constructor which will use the database instance provided for storage
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="sqlSyntaxProvider"></param>
        /// <param name="db"></param>
        public ExternalLoginStore(ILogger logger, ISqlSyntaxProvider sqlSyntaxProvider, UmbracoDatabase db)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (sqlSyntaxProvider == null) throw new ArgumentNullException(nameof(sqlSyntaxProvider));
            if (db == null) throw new ArgumentNullException(nameof(db));

            _logger = logger;
            _sqlSyntaxProvider = sqlSyntaxProvider;
            _db = db;
            _useSeparateDbFile = false;
        }
       
        private void EnsureInitialized()
        {
            LazyInitializer.EnsureInitialized(ref _dbInitialized, ref _isInitialized, ref _locker, () =>
            {
                EnsureSqlCe();
                EnsureTable();
                return true;
            });            
        }

        /// <summary>
        /// Checks if the table exists and if not it creates it
        /// </summary>
        private void EnsureTable()
        {
            var schemaHelper = new DatabaseSchemaHelper(_db, _logger, _sqlSyntaxProvider);
            if (!schemaHelper.TableExist(TableName))
            {            
                var tableExist = schemaHelper.TableExist(TableName);
                
                if (tableExist == false)
                {
                    using (var transaction = _db.GetTransaction())
                    {
                        schemaHelper.CreateTable<ExternalLoginDto>();
                        transaction.Complete();
                    }
                }

                LogHelper.Info<ExternalLoginStore>(string.Format("New table '{0}' was created", TableName));
            }
        }

        /// <summary>
        /// If SqlCe is used then ensure the db exists - for backwards compat
        /// </summary>
        private void EnsureSqlCe()
        {
            if (_useSeparateDbFile == false) return;
            if (!(_sqlSyntaxProvider is SqlCeSyntaxProvider)) return;

            if (!System.IO.File.Exists(IOHelper.MapPath("~/App_Data/UmbracoIdentity.sdf")))
            {
                using (var en = new SqlCeEngine(ConnString))
                {
                    en.CreateDatabase();
                }
            }
        }

        public IEnumerable<IdentityMemberLogin<int>> GetAll(int userId)
        {
            EnsureInitialized();

            var sql = new Sql()
                .Select("*")
                .From<ExternalLoginDto>(_sqlSyntaxProvider)
                .Where<ExternalLoginDto>(dto => dto.UserId == userId);

            var found = _db.Fetch<ExternalLoginDto>(sql);

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

            var sql = new Sql() 
                .Select("*")
                .From<ExternalLoginDto>(_sqlSyntaxProvider)
                .Where<ExternalLoginDto>(dto => dto.LoginProvider == login.LoginProvider && dto.ProviderKey == login.ProviderKey);

            var found = _db.Fetch<ExternalLoginDto>(sql);

            return found.Select(x => x.UserId);
        }

        public void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins)
        {
            EnsureInitialized();

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

        public void DeleteUserLogins(int memberId)
        {
            EnsureInitialized();

            using (var t = _db.GetTransaction())
            {
                _db.Execute("DELETE FROM ExternalLogins WHERE UserId=@userId", new {userId = memberId});

                t.Complete();
            }
        }

        protected override void DisposeResources()
        {
            _db.Dispose();
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
