using System;

namespace UmbracoIdentity.Models
{

    public class IdentityMemberRole : IdentityMemberRole<int> { }

    /// <summary>
    ///     EntityType that represents a user belonging to a role
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class IdentityMemberRole<TKey>
    {
        /// <summary>
        ///     UserId for the user that is in the role
        /// </summary>
        public virtual TKey UserId { get; set; }

        /// <summary>
        /// RoleId for the role
        /// </summary>
        public virtual string RoleName { get; set; }
    }
}