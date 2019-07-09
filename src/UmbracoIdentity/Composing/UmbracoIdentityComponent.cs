using System;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Models;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using UmbracoIdentity.Migrations;

namespace UmbracoIdentity.Composing
{
    public class UmbracoIdentityComponent : IComponent
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMigrationBuilder _migrationBuilder;
        private readonly IKeyValueService _keyValueService;
        private readonly ILogger _logger;

        public UmbracoIdentityComponent(
            IScopeProvider scopeProvider,
            IMigrationBuilder migrationBuilder,
            IKeyValueService keyValueService,
            ILogger logger)
        {
            _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
            _migrationBuilder = migrationBuilder ?? throw new ArgumentNullException(nameof(migrationBuilder));
            _keyValueService = keyValueService ?? throw new ArgumentNullException(nameof(keyValueService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Initialize()
        {
            EnsureDatabaseSchema();

            MemberService.Saving += MemberService_Saving;
        }

        public void Terminate()
        {

        }

        /// <summary>
        /// Executes a db migration to install the UmbracoIdentity DB schema if required
        /// </summary>
        private void EnsureDatabaseSchema()
        {
            // migrate database schema
            var upgrader = new Upgrader(new UmbracoIdentityMigrationPlan());

            // check if we need to execute
            // TODO: This logic should be better built into Umbraco (i.e. Package Migrations)
            var stateValueKey = upgrader.StateValueKey;
            var currMigrationState = _keyValueService.GetValue(stateValueKey);
            var finalMigrationState = upgrader.Plan.FinalState;
            var needsUpgrade = currMigrationState != finalMigrationState;
            _logger.Debug<UmbracoIdentityComponent>("Final upgrade state is {FinalMigrationState}, database contains {DatabaseState}, needs upgrade? {NeedsUpgrade}", finalMigrationState, currMigrationState ?? "<null>", needsUpgrade);

            if (needsUpgrade)
            {
                upgrader.Execute(_scopeProvider, _migrationBuilder, _keyValueService, _logger);
            }
        }

        /// <summary>
        /// Listen for when Members is being saved, check if it is a
        /// new item and fill in some default data.
        /// </summary>
        private void MemberService_Saving(IMemberService sender, SaveEventArgs<IMember> e)
        {
            foreach (var member in e.SavedEntities)
            {
                var securityStamp = member.GetSecurityStamp(out var propertyTypeExists);
                if (!propertyTypeExists)
                {
                    _logger.Warn<UmbracoIdentityComponent>($"The {UmbracoIdentityConstants.SecurityStampProperty} does not exist on the member type {member.ContentType.Alias}, see docs on how to fix: https://github.com/Shazwazza/UmbracoIdentity/wiki");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(securityStamp))
                    {
                        //set one, it shouldn't be empty
                        member.SetValue(UmbracoIdentityConstants.SecurityStampProperty, Guid.NewGuid().ToString());
                    }
                }
            }
        }
    }
}
