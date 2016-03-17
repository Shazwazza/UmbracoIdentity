using Microsoft.AspNet.Identity;

namespace UmbracoIdentity.Models
{
    /// <summary>
    /// Default IRole implementation
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class IdentityRole<TKey> : IRole<TKey>
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public virtual TKey Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public virtual string Name { get; set; }
    }
}