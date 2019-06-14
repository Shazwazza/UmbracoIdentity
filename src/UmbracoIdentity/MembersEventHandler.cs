using System;
using System.Web;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Services.Implement;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    /// <summary>
    /// Binds to Umbraco member events for each OwinContext
    /// </summary>
    public sealed class MembersEventHandler<T> : DisposableObjectSlim
         where T : UmbracoIdentityMember, new()
    {
        private readonly IOwinContext _owinContext;

        public MembersEventHandler(IOwinContext owinContext)
        {
            _owinContext = owinContext;
            MemberService.Deleted += MemberService_Deleted;
        }

        /// <summary>
        /// When a member is deleted we should remove the associated logins
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MemberService_Deleted(IMemberService sender, Umbraco.Core.Events.DeleteEventArgs<Umbraco.Core.Models.IMember> e)
        {
            var userMgr = _owinContext.GetUserManager<UmbracoMembersUserManager<T>>();
            if (userMgr != null)
            {
                foreach (var member in e.DeletedEntities)
                {
                    //delete the member from identity (this will already be removed from the members table, but this will ensure that
                    // the users' external logins are removed too.
                    userMgr.DeleteAsync(new T()
                    {
                        Id = member.Id,
                        Name = member.Name,
                        UserName = member.Username,
                        Email = member.Email                        
                    });
                }                
            }    
        }

        /// <summary>
        /// Unbind from event
        /// </summary>
        protected override void DisposeResources()
        {
            MemberService.Deleted -= MemberService_Deleted;
        }
    }
}