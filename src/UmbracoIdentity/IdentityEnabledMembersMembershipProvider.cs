using System.Collections.Specialized;
using System.Web.Configuration;
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
        /// <summary>
        /// Gets a value indicating whether passwords require a digit.
        /// </summary>
        /// <value></value>
        /// <returns>true if a password requires a digit; otherwise, false. The default is false.</returns>
        public bool PasswordRequiresDigit { get; private set; }

        /// <summary>
        /// Gets a value indicating whether passwords require a lowercase character.
        /// </summary>
        /// <value></value>
        /// <returns>true if a password requires a lowercase character; otherwise, false. The default is false.</returns>
        public bool PasswordRequiresLowercase { get; private set; }

        /// <summary>
        /// Gets a value indicating whether passwords require a uppercase character.
        /// </summary>
        /// <value></value>
        /// <returns>true if a password requires a uppercase character; otherwise, false. The default is false.</returns>
        public bool PasswordRequiresUppercase { get; private set; }

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

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);

            PasswordRequiresDigit = config.GetValue("passwordRequiresDigit", false);
            PasswordRequiresLowercase = config.GetValue("passwordRequiresLowercase", false);
            PasswordRequiresUppercase = config.GetValue("passwordRequiresUppercase", false);
        }
    }
}
