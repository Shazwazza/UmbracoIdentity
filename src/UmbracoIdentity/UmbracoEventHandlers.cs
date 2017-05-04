using System;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

namespace UmbracoIdentity
{
    public sealed class UmbracoEventHandlers :  ApplicationEventHandler
    {
        /// <summary>
        /// We wan to bind to member saving so we can generate a security stamp if one doesn't exist
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            MemberService.Saving += MemberService_Saving;
        }

        private void MemberService_Saving(IMemberService sender, Umbraco.Core.Events.SaveEventArgs<Umbraco.Core.Models.IMember> e)
        {
            foreach (var member in e.SavedEntities)
            {
                var securityStamp = member.GetSecurityStamp(out bool propertyTypeExists);
                if (!propertyTypeExists)
                {
                    LogHelper.Warn<UmbracoEventHandlers>($"The {UmbracoIdentityConstants.SecurityStampProperty} does not exist on the member type {member.ContentType.Alias}, see docs on how to fix: https://github.com/Shazwazza/UmbracoIdentity/wiki");
                }
                else
                {
                    if (securityStamp.IsNullOrWhiteSpace())
                    {
                        //set one, it shouldn't be empty
                        member.SetValue(UmbracoIdentityConstants.SecurityStampProperty, Guid.NewGuid().ToString());
                    }
                }
            }
        }
        
    }
}