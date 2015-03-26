using System;
using System.Collections.Generic;
using Microsoft.AspNet.Identity;
using UmbracoIdentity.Models;


namespace UmbracoIdentity
{
    /// <summary>
    /// Used to store the external login info, this can be replaced with your own implementation
    /// </summary>
    public interface IExternalLoginStore : IDisposable
    {
        /// <summary>
        /// Returns all user logins assigned
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        IEnumerable<IdentityMemberLogin<int>> GetAll(int userId);
        
        /// <summary>
        /// Returns all logins matching the login info - generally there should only be one but in some cases 
        /// there might be more than one depending on if an adminstrator has been editing/removing members
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        IEnumerable<int> Find(UserLoginInfo login);

        /// <summary>
        /// Save user logins
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="logins"></param>
        void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins);

        /// <summary>
        /// Deletes all user logins - normally used when a member is deleted
        /// </summary>
        /// <param name="memberId"></param>
        void DeleteUserLogins(int memberId);
    }
}