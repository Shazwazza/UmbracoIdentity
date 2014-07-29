using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity.EntityFramework;

namespace UmbracoIdentity
{
    public class UmbracoIdentityUser : IdentityUser<int, IdentityUserLogin<int>, IdentityUserRole<int>, IdentityUserClaim<int>>
    {
        /// <summary>
        /// Gets/sets the members real name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Overridden to make the retrieval lazy
        /// </summary>
        public override ICollection<IdentityUserLogin<int>> Logins
        {
            get
            {
                if (_getLogins != null && !base.Logins.Any())
                {
                    foreach (var l in _getLogins.Value)
                    {
                        base.Logins.Add(l);
                    }
                }
                return base.Logins;
            }
        }

        private Lazy<IEnumerable<IdentityUserLogin<int>>> _getLogins;

        /// <summary>
        /// Used to set a lazy call back to populate the user's Login list
        /// </summary>
        /// <param name="callback"></param>
        public void SetLoginsCallback(Lazy<IEnumerable<IdentityUserLogin<int>>> callback)
        {
            if (callback == null) throw new ArgumentNullException("callback");
            _getLogins = callback;
        }
    }
}