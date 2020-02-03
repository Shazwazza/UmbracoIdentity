using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Umbraco.Web.Composing;
using Umbraco.Core;
using Microsoft.AspNet.Identity;
using UmbracoIdentity;

namespace UmbracoIdentity.Web.Models.UmbracoIdentity
{
    public class UserPasswordModel : IValidatableObject
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]        
        public string ConfirmPassword { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var userMgr = Current.Factory.GetInstance<UmbracoMembersUserManager<UmbracoApplicationMember>>();

            if (userMgr.PasswordValidator is PasswordValidator pwordValidator && NewPassword.Length < pwordValidator.RequiredLength)
                yield return new ValidationResult($"Password must be at least {pwordValidator.RequiredLength} characters long.", new[] { nameof(NewPassword) });

        }
    }
}
