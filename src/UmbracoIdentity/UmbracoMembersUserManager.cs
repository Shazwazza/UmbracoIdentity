using System;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Core.Services;
using UmbracoIdentity.Models;

namespace UmbracoIdentity
{
    /// <summary>
    /// A custom user manager that uses the UmbracoMembersUserStore
    /// </summary>
    public class UmbracoMembersUserManager<TUser> : UserManager<TUser, int>
        where TUser : UmbracoIdentityMember, IUser<int>, new()
    {

        public UmbracoMembersUserManager(IUserStore<TUser, int> store)
            : base(store)
        {
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
        
        public override bool SupportsUserSecurityStamp => true;

        public override bool SupportsUserTwoFactor
        {
            get { return false; }
        }

        public override bool SupportsUserPhoneNumber
        {
            get { return false; }
        }

        /// <summary>
        /// Default method to create a user manager
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="scopeProvider"></param>
        /// <param name="memberService"></param>
        /// <param name="memberTypeService"></param>
        /// <param name="memberGroupService"></param>
        /// <param name="membershipProvider"></param>
        /// <returns></returns>
        public static UmbracoMembersUserManager<TUser> Create(
            IdentityFactoryOptions<UmbracoMembersUserManager<TUser>> options,
            ILogger logger,
            IScopeProvider scopeProvider,
            IMemberService memberService,
            IMemberTypeService memberTypeService,
            IMemberGroupService memberGroupService,            
            IdentityEnabledMembersMembershipProvider membershipProvider = null)
        {
            //we'll grab some settings from the membership provider
            var provider = membershipProvider ?? Membership.Providers["UmbracoMembershipProvider"] as IdentityEnabledMembersMembershipProvider;

            if (provider == null)
            {
                throw new InvalidOperationException("In order to use " + typeof(UmbracoMembersUserManager<>) + " the Umbraco members membership provider must be of type " + typeof(IdentityEnabledMembersMembershipProvider));
            }
            
            var externalLoginStore = new ExternalLoginStore(scopeProvider);

            return Create(options, new UmbracoMembersUserStore<TUser>(logger, memberService, memberTypeService, memberGroupService, provider, externalLoginStore), membershipProvider);
        }

        /// <summary>
        /// Method to create a user manager which can be used to specify a custom IExternalLoginStore
        /// </summary>
        /// <param name="options"></param>
        /// <param name="memberService"></param>
        /// <param name="memberTypeService"></param>
        /// <param name="memberGroupService"></param>
        /// <param name="externalLoginStore"></param>
        /// <param name="membershipProvider"></param>
        /// <returns></returns>
        public static UmbracoMembersUserManager<TUser> Create(
            IdentityFactoryOptions<UmbracoMembersUserManager<TUser>> options, 
            IMemberService memberService,
            IMemberTypeService memberTypeService,
            IMemberGroupService memberGroupService,
            IProfilingLogger profilingLogger,
            IScopeProvider scopeProvider,
            IExternalLoginStore externalLoginStore = null,
            IdentityEnabledMembersMembershipProvider membershipProvider = null)
        {
            //we'll grab some settings from the membership provider
            var provider = membershipProvider ?? Membership.Providers["UmbracoMembershipProvider"] as IdentityEnabledMembersMembershipProvider;

            if (provider == null)
            {
                throw new InvalidOperationException("In order to use " + typeof(UmbracoMembersUserManager<>) + " the Umbraco members membership provider must be of type " + typeof(IdentityEnabledMembersMembershipProvider));
            }

            if (externalLoginStore == null)
            {
                //use the default
                externalLoginStore = new ExternalLoginStore(scopeProvider);
            }

            return Create(options, new UmbracoMembersUserStore<TUser>(profilingLogger, memberService, memberTypeService, memberGroupService, provider, externalLoginStore), membershipProvider);
        }

        /// <summary>
        /// Used to create a user manager with custom store
        /// </summary>
        /// <param name="options"></param>
        /// <param name="customUserStore"></param>
        /// <param name="membershipProvider"></param>
        /// <returns></returns>
        public static UmbracoMembersUserManager<TUser> Create(
          IdentityFactoryOptions<UmbracoMembersUserManager<TUser>> options,
          UmbracoMembersUserStore<TUser> customUserStore,
          IdentityEnabledMembersMembershipProvider membershipProvider = null)
        {

            //we'll grab some settings from the membership provider
            var provider = membershipProvider ?? Membership.Providers["UmbracoMembershipProvider"] as IdentityEnabledMembersMembershipProvider;

            if (provider == null)
            {
                throw new InvalidOperationException("In order to use " + typeof(UmbracoMembersUserManager<>) + " the Umbraco members membership provider must be of type " + typeof(IdentityEnabledMembersMembershipProvider));
            }

            var manager = new UmbracoMembersUserManager<TUser>(customUserStore);

            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<TUser, int>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = provider.MinRequiredPasswordLength,
                RequireNonLetterOrDigit = provider.MinRequiredNonAlphanumericCharacters > 0,
                RequireDigit = provider.PasswordRequiresDigit,
                RequireLowercase = provider.PasswordRequiresLowercase,
                RequireUppercase = provider.PasswordRequiresUppercase
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
                manager.UserTokenProvider = new DataProtectorTokenProvider<TUser, int>(dataProtectionProvider.Create("ASP.NET Identity"));
            }

            return manager;
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
