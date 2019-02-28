using System;
using Umbraco.Core.Composing;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;

namespace UmbracoIdentity.Composing
{
    public class UmbracoIdentityComponent : IComponent
    {
        public void Initialize()
        {
            //var upgrader = new Upgrader(new IdentityMigrationPlan());
            //upgrader.Execute(_scopeProvider, _migrationBuilder, _keyValueService, _logger);

            MemberService.Saving += MemberService_Saving;
        }

        public void Terminate()
        {

        }

        /// <summary>
        /// Listen for when Members is being saved, check if it is a
        /// new item and fill in some default data.
        /// </summary>
        private static void MemberService_Saving(IMemberService sender, SaveEventArgs<IMember> e)
        {
            foreach (var member in e.SavedEntities)
            {
                var securityStamp = member.GetSecurityStamp(out var propertyTypeExists);
                if (!propertyTypeExists)
                {
                    Current.Logger.Warn<UmbracoIdentityComponent>($"The {UmbracoIdentityConstants.SecurityStampProperty} does not exist on the member type {member.ContentType.Alias}, see docs on how to fix: https://github.com/Shazwazza/UmbracoIdentity/wiki");
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
