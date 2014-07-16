using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Umbraco.Web;
using UmbracoIdentity.Web.Models;
using UmbracoIdentity.Web;
using Umbraco.Core;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using UmbracoIdentity;
using IdentityExtensions = UmbracoIdentity.IdentityExtensions;

namespace UmbracoIdentity.Web.Controllers
{
    [Authorize]
    public class UmbracoIdentityAccountController : SurfaceController
    {
        private UmbracoMembersUserManager<ApplicationUser> _userManager;
        
        protected IOwinContext OwinContext
        {
            get { return HttpContext.GetOwinContext(); }
        }

        public UmbracoMembersUserManager<ApplicationUser> UserManager
        {
            get
            {
                return _userManager ?? (_userManager = HttpContext.GetOwinContext()
                    .GetUserManager<UmbracoMembersUserManager<ApplicationUser>>());
            }
        }

        #region External login and registration

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider,
                Url.SurfaceAction<UmbracoIdentityAccountController>("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await OwinContext.Authentication.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                //go home, invalid callback
                return RedirectToLocal("/");
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;

                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider,
                Url.SurfaceAction<UmbracoIdentityAccountController>("LinkLoginCallback"),
                User.Identity.GetUserId());
        }

        [HttpGet]
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId<int>(), loginInfo.Login);
            if (result.Succeeded)
            {
                return RedirectToAction("Manage");
            }
            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                //go home, already authenticated
                return RedirectToLocal("/");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await OwinContext.Authentication.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }

                var user = new ApplicationUser()
                {
                    Name = info.ExternalIdentity.Name,
                    UserName = model.Email,
                    Email = model.Email
                };

                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInAsync(user, isPersistent: false);

                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link
                        // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        // SendEmail(user.Email, callbackUrl, "Confirm your account", "Please confirm your account by clicking this link");

                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        } 

        #endregion

        #region Standard login and registration

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> HandleLogin([Bind(Prefix = "loginModel")] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindAsync(model.Username, model.Password);
                if (user != null)
                {
                    await SignInAsync(user, true);
                    return RedirectToCurrentUmbracoPage();
                }
                ModelState.AddModelError("loginModel", "Invalid username or password");
            }

            return CurrentUmbracoPage();
        }

        [HttpPost]
        public ActionResult HandleLogout([Bind(Prefix = "logoutModel")]PostRedirectModel model)
        {
            if (ModelState.IsValid == false)
            {
                return CurrentUmbracoPage();
            }

            if (Members.IsLoggedIn())
            {
                OwinContext.Authentication.SignOut();
            }

            //if there is a specified path to redirect to then use it
            if (model.RedirectUrl.IsNullOrWhiteSpace() == false)
            {
                return Redirect(model.RedirectUrl);
            }

            //redirect to current page by default
            TempData["LogoutSuccess"] = true;
            return RedirectToCurrentUmbracoPage();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> HandleRegisterMember([Bind(Prefix = "registerModel")]RegisterModel model)
        {

            if (ModelState.IsValid == false)
            {
                return CurrentUmbracoPage();
            }

            var user = new ApplicationUser()
            {
                UserName = model.UsernameIsEmail || model.Username == null ? model.Email : model.Username,
                Email = model.Email
            };

            var result = await UserManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await SignInAsync(user, isPersistent: false);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                //if there is a specified path to redirect to then use it
                if (model.RedirectUrl.IsNullOrWhiteSpace() == false)
                {
                    return Redirect(model.RedirectUrl);
                }
                //redirect to current page by default
                TempData["FormSuccess"] = true;
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                AddErrors(result, "registerModel");
            }

            return CurrentUmbracoPage();
        } 

        #endregion

        #region Helpers

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            OwinContext.Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            OwinContext.Authentication.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent },
                await user.GenerateUserIdentityAsync(UserManager));
        }

        private void AddErrors(IdentityResult result, string prefix = "")
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(prefix, error);
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/");
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri, string userId = null)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            private string LoginProvider { get; set; }
            private string RedirectUri { get; set; }
            private string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}