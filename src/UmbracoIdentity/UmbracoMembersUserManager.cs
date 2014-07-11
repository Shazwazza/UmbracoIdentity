using System;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Umbraco.Core.Services;

namespace UmbracoIdentity
{
    /// <summary>
    /// A custom user manager that uses the UmbracoMembersUserStore
    /// </summary>
    public class UmbracoMembersUserManager<T> : UserManager<T, int>
        where T : UmbracoIdentityUser, IUser<int>, new()
    {
        public UmbracoMembersUserManager(IUserStore<T, int> store)
            : base(store)
        {
        }

        //TODO: Support this
        public override bool SupportsUserRole
        {
            get { return false; }
        }

        //TODO: Support this
        public override bool SupportsQueryableUsers
        {
            get { return false; }
        }

        //TODO: Support this
        public override bool SupportsUserLockout
        {
            get { return false;  }
        }

        //TODO: Support this
        public override bool SupportsUserSecurityStamp
        {
            get { return false; }
        }


        public override bool SupportsUserTwoFactor
        {
            get { return false; }
        }

        public override bool SupportsUserPhoneNumber
        {
            get { return false; }
        }

        public static UmbracoMembersUserManager<T> Create(
            IdentityFactoryOptions<UmbracoMembersUserManager<T>> options, 
            IOwinContext context,
            IMemberService memberService)
        {

            //we'll grab some settings from the membership provider
            var provider = Membership.Providers["UmbracoMembershipProvider"] as IdentityEnabledMembersMembershipProvider;

            if (provider == null)
            {
                throw new InvalidOperationException("In order to use " + typeof(UmbracoMembersUserManager<>) + " the Umbraco members membership provider must be of type " + typeof(IdentityEnabledMembersMembershipProvider));
            }

            var manager = new UmbracoMembersUserManager<T>(new UmbracoMembersUserStore<T>(memberService, provider));

            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<T, int>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            
            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = provider.MinRequiredPasswordLength,
                RequireNonLetterOrDigit = provider.MinRequiredNonAlphanumericCharacters > 0,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false                
            };

            //use a custom hasher based on our membership provider
            manager.PasswordHasher = new MembershipPasswordHasher(provider);

            //NOTE: Not implementing these currently

            //// Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            //// You can write your own provider and plug in here.
            //manager.RegisterTwoFactorProvider("PhoneCode", new PhoneNumberTokenProvider<ApplicationUser>
            //{
            //    MessageFormat = "Your security code is: {0}"
            //});
            //manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider<ApplicationUser>
            //{
            //    Subject = "Security Code",
            //    BodyFormat = "Your security code is: {0}"
            //});
            
            //manager.EmailService = new EmailService();
            //manager.SmsService = new SmsService();

            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<T, int>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }
}