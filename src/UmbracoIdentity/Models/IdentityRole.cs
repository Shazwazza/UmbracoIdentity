using System;
using System.Collections.Generic;
using Microsoft.AspNet.Identity;
using Umbraco.Core.Models;

namespace UmbracoIdentity.Models
{
    /// <summary>
    /// Default IRole implementation
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class IdentityRole<TKey> : IRole<TKey>
    {
        public virtual TKey Id { get; private set; }

        public virtual string Name { get; set; }
    }
}