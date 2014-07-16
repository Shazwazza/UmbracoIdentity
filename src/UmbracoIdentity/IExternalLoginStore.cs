using System;
using System.Collections.Generic;
using Microsoft.AspNet.Identity;

namespace UmbracoIdentity
{
    /// <summary>
    /// Used to store the external login info, this can be replaced with your own implementation
    /// </summary>
    public interface IExternalLoginStore : IDisposable
    {
        int? Find(UserLoginInfo login);
        void SaveUserLogins(int memberId, IEnumerable<UserLoginInfo> logins);
    }
}