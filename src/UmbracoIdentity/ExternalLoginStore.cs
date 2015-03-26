using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;
using Umbraco.Core.Persistence.SqlSyntax;
using Umbraco.Core.Services;

namespace UmbracoIdentity
{
    /// <summary>
    /// We are using a custom sqlce db to store this information
    /// </summary>
    internal class ExternalLoginStore : DisposableObject, IExternalLoginStore
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
            if (!_db.TableExist("ExternalLogins"))
            {
                //unfortunately we'll get issues if we just try this because of differing sql syntax providers. In newer
                // umbraco versions we'd just use the DatabaseSchemaHelper. So in the meantime we have to just 
                // do this manually and use reflection :(;
                //_db.CreateTable<ExternalLoginDto>();

                var sqlceProvider = new SqlCeSyntaxProvider();
                CreateTable(false, typeof (ExternalLoginDto), sqlceProvider);
            }
        }

        public IEnumerable<IdentityUserLogin<int>> GetAll(int userId)
        {
            var sql = new Sql()
                .Select("*")
                .From<ExternalLoginDto>()
                .Where<ExternalLoginDto>(dto => dto.UserId == userId);

            var found = _db.Fetch<ExternalLoginDto>(sql);

            return found.Select(x => new IdentityUserLogin<int>
            {
                LoginProvider = x.LoginProvider,
                ProviderKey = x.ProviderKey,
                UserId = x.UserId
            });
        }

        public IEnumerable<int> Find(UserLoginInfo login)
        {
            var sql = new Sql() 
                .Select("*")
                .From<ExternalLoginDto>()
                .Where<ExternalLoginDto>(dto => dto.LoginProvider == login.LoginProvider && dto.ProviderKey == login.ProviderKey);

            var found = _db.Fetch<ExternalLoginDto>(sql);

            return found.Select(x => x.UserId);
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

        public void DeleteUserLogins(int memberId)
        {
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

        private void CreateTable(bool overwrite, Type modelType, SqlCeSyntaxProvider syntaxProvider)
        {
            var defFactoryType = Type.GetType("Umbraco.Core.Persistence.DatabaseModelDefinitions.DefinitionFactory,Umbraco.Core", true);
            var tableDefinition = (TableDefinition)defFactoryType.CallStaticMethod("GetTableDefinition", modelType);

            var tableName = tableDefinition.Name;

            string createSql = syntaxProvider.Format(tableDefinition);
            string createPrimaryKeySql = syntaxProvider.FormatPrimaryKey(tableDefinition);
            var foreignSql = syntaxProvider.Format(tableDefinition.ForeignKeys);
            var indexSql = syntaxProvider.Format(tableDefinition.Indexes);

            var tableExist = _db.TableExist(tableName);
            if (overwrite && tableExist)
            {
                _db.DropTable(tableName);
                tableExist = false;
            }

            if (tableExist == false)
            {
                using (var transaction = _db.GetTransaction())
                {
                    //Execute the Create Table sql
                    int created = _db.Execute(new Sql(createSql));
                    LogHelper.Info<ExternalLoginStore>(string.Format("Create Table sql {0}:\n {1}", created, createSql));

                    //If any statements exists for the primary key execute them here
                    if (!string.IsNullOrEmpty(createPrimaryKeySql))
                    {
                        int createdPk = _db.Execute(new Sql(createPrimaryKeySql));
                        LogHelper.Info<ExternalLoginStore>(string.Format("Primary Key sql {0}:\n {1}", createdPk, createPrimaryKeySql));
                    }

                    //Turn on identity insert if db provider is not mysql
                    if (syntaxProvider.SupportsIdentityInsert() && tableDefinition.Columns.Any(x => x.IsIdentity))
                        _db.Execute(new Sql(string.Format("SET IDENTITY_INSERT {0} ON ", syntaxProvider.GetQuotedTableName(tableName))));
                    
                    //Turn off identity insert if db provider is not mysql
                    if (syntaxProvider.SupportsIdentityInsert() && tableDefinition.Columns.Any(x => x.IsIdentity))
                        _db.Execute(new Sql(string.Format("SET IDENTITY_INSERT {0} OFF;", syntaxProvider.GetQuotedTableName(tableName))));
                    
                    //Loop through foreignkey statements and execute sql
                    foreach (var sql in foreignSql)
                    {
                        int createdFk = _db.Execute(new Sql(sql));
                        LogHelper.Info<ExternalLoginStore>(string.Format("Create Foreign Key sql {0}:\n {1}", createdFk, sql));
                    }

                    //Loop through index statements and execute sql
                    foreach (var sql in indexSql)
                    {
                        int createdIndex = _db.Execute(new Sql(sql));
                        LogHelper.Info<ExternalLoginStore>(string.Format("Create Index sql {0}:\n {1}", createdIndex, sql));
                    }

                    transaction.Complete();
                }
            }

            LogHelper.Info<ExternalLoginStore>(string.Format("New table '{0}' was created", tableName));
        }
    }
}
