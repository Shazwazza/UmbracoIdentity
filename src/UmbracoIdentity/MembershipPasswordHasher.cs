using Microsoft.AspNet.Identity;

namespace UmbracoIdentity
{
    /// <summary>
    /// A custom password hasher that conforms to the current password hashing done in Umbraco
    /// </summary>
    public class MembershipPasswordHasher : IPasswordHasher
    {
        private readonly IdentityEnabledMembersMembershipProvider _provider;

        public MembershipPasswordHasher(IdentityEnabledMembersMembershipProvider provider)
        {
            _provider = provider;
        }

        public string HashPassword(string password)
        {
            return _provider.HashPasswordForStorage(password);
        }

        public PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            return _provider.VerifyPassword(providedPassword, hashedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }

       
    }
}