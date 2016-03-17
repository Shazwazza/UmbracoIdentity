using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace UmbracoIdentity.Web.Models.UmbracoIdentity
{
    public class UserPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]        
        public string ConfirmPassword { get; set; }
    }
}