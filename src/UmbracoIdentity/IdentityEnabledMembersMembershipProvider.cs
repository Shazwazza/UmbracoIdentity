using Umbraco.Web.Security.Providers;

namespace UmbracoIdentity
{

    /// <summary>
    /// A custom membership provider that inherits from the default Umbraco ones that allows
    /// the use of the currently implemented password hashing mechanisms used in Umbraco.
    /// </summary>
    public class IdentityEnabledMembersMembershipProvider : MembersMembershipProvider
    {
        public string HashPasswordForStorage(string password)
        {
            string salt;
            var hashed = EncryptOrHashNewPassword(password, out salt);
            return FormatPasswordForStorage(hashed, salt);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return CheckPassword(password, hashedPassword);
        }
    }
}