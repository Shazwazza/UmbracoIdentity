using System.Web.Security;
using Umbraco.Web.Security.Providers;

namespace UmbracoIdentity
{

    /// <summary>
    /// A custom membership provider that inherits from the default Umbraco ones that allows
    /// the use of the currently implemented password hashing mechanisms used in Umbraco.
    /// </summary>
    public class IdentityEnabledMembersMembershipProvider : MembersMembershipProvider
    {
        public new string HashPasswordForStorage(string password)
        {
            string salt;
            var hashed = EncryptOrHashNewPassword(password, out salt);
            return FormatPasswordForStorage(hashed, salt);
        }

        public new bool VerifyPassword(string password, string hashedPassword)
        {
            return CheckPassword(password, hashedPassword);
        }

        public override MembershipPasswordFormat PasswordFormat => MembershipPasswordFormat.Hashed;
    }
}