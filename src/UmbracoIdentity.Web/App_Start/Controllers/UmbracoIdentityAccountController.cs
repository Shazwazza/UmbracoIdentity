﻿using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using UmbracoIdentity.Models;

namespace UmbracoIdentity.Web.Controllers
{
    using Models.UmbracoIdentity;

    [Authorize]
    public class UmbracoIdentityAccountController : SurfaceController
    {
        private UmbracoMembersUserManager<UmbracoApplicationMember> _userManager;
        private UmbracoMembersRoleManager<UmbracoApplicationRole> _roleManager;

        public UmbracoIdentityAccountController(
            UmbracoMembersUserManager<UmbracoApplicationMember> userManager,
            UmbracoMembersRoleManager<UmbracoApplicationRole> roleManager,
            IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory databaseFactory, ServiceContext services, AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper)
            : base(umbracoContextAccessor, databaseFactory, services, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        protected IOwinContext OwinContext
        {
            get { return Request.GetOwinContext(); }
        }

        #region External login and registration

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            if (returnUrl.IsNullOrWhiteSpace())
            {
                returnUrl = Request.RawUrl;
            }

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
                return RedirectToLocal(returnUrl);
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await _userManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }

            if (loginInfo.Email.IsNullOrWhiteSpace())
            {
                ViewBag.Description = "No email address found in the claims, ensure your OAuth provider is configured to return the Email address";
                return View("ExternalLoginFailure");
            }

            // If the user does not have an account, then create one

            user = new UmbracoApplicationMember()
            {
                Name = loginInfo.ExternalIdentity.Name,
                UserName = loginInfo.Email,
                Email = loginInfo.Email
            };

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user.Id, loginInfo.Login);
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

            //something went wrong
            AddModelErrors(result);
            return View("ExternalLoginFailure");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider, string returnUrl = null)
        {
            if (returnUrl.IsNullOrWhiteSpace())
            {
                returnUrl = Request.RawUrl;
            }

            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider,
                Url.SurfaceAction<UmbracoIdentityAccountController>("LinkLoginCallback", new { ReturnUrl = returnUrl }),
                User.Identity.GetUserId());
        }

        [HttpGet]
        public async Task<ActionResult> LinkLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                TempData["LinkLoginError"] = new[] { "An error occurred, could not get external login info" };
                return RedirectToLocal(returnUrl);
            }
            var result = await _userManager.AddLoginAsync(User.Identity.GetUserId<int>(), loginInfo.Login);
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            TempData["LinkLoginError"] = result.Errors.ToArray();
            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            var result = await _userManager.RemoveLoginAsync(
                User.Identity.GetUserId<int>(),
                new UserLoginInfo(loginProvider, providerKey));

            if (result.Succeeded)
            {
                var user = await _userManager.FindByIdAsync(User.Identity.GetUserId<int>());
                await SignInAsync(user, isPersistent: false);
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                AddModelErrors(result);
                return CurrentUmbracoPage();
            }
        }

        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = _userManager.GetLogins(User.Identity.GetUserId<int>());
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return PartialView(linkedAccounts);
        }

        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) throw new ArgumentNullException("role cannot be null");

            var user = await _userManager.FindByIdAsync(User.Identity.GetUserId<int>());
            if (user != null)
            {
                var found = user.Roles.FirstOrDefault(x => x.RoleName == roleName);
                if (found != null)
                {
                    user.Roles.Remove(found);
                }
                else
                {   
                    user.Roles.Add(new IdentityMemberRole {RoleName = roleName, UserId = user.Id});
                }
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToCurrentUmbracoPage();
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                    return CurrentUmbracoPage();
                }
            }

            return RedirectToCurrentUmbracoPage();
        }

        [ChildActionOnly]
        public async Task<ActionResult> ShowRoles()
        {
            var user = await _userManager.FindByIdAsync(User.Identity.GetUserId<int>());

            var model = new RoleManagementModel();

            if (user != null)
            {
                model.AssignedRoles = user.Roles.Select(x => x.RoleName);
                model.AvailableRoles = await _roleManager.GetAll();
            }

            return PartialView("ShowRoles", model);
        }

        [ChildActionOnly]
        public ActionResult ManagePassword()
        {
            ViewBag.HasLocalPassword = HasPassword();
            return View();
        }

        [NotChildAction]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ManagePassword([Bind(Prefix = "managePasswordModel")] UserPasswordModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;

            //vaidate their passwords match
            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("managePasswordModel.ConfirmPassword", "Passwords do not match");
            }

            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await _userManager.ChangePasswordAsync(User.Identity.GetUserId<int>(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        var user = await _userManager.FindByIdAsync(User.Identity.GetUserId<int>());
                        await SignInAsync(user, isPersistent: false);
                        TempData["ChangePasswordSuccess"] = true;
                        return RedirectToCurrentUmbracoPage();
                    }
                    else
                    {
                        AddModelErrors(result, "managePasswordModel");
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                var state = ModelState["managePasswordModel.OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await _userManager.AddPasswordAsync(User.Identity.GetUserId<int>(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        TempData["ChangePasswordSuccess"] = true;
                        return RedirectToCurrentUmbracoPage();
                    }
                    else
                    {
                        AddModelErrors(result, "managePasswordModel");
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return CurrentUmbracoPage();
        }

        #region Standard login and registration

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> HandleLogin([Bind(Prefix = "loginModel")] LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindAsync(model.Username, model.Password);
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
                //ensure to only clear the default cookies
                OwinContext.Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie, DefaultAuthenticationTypes.ExternalCookie);
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

            var user = new UmbracoApplicationMember()
            {
                UserName = model.UsernameIsEmail || model.Username == null ? model.Email : model.Username,
                Email = model.Email,
                MemberProperties = model.MemberProperties,
                MemberTypeAlias = model.MemberTypeAlias
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await SignInAsync(user, isPersistent: false);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                TempData["FormSuccess"] = true;

                //if there is a specified path to redirect to then use it
                if (model.RedirectUrl.IsNullOrWhiteSpace() == false)
                {
                    return Redirect(model.RedirectUrl);
                }
                //redirect to current page by default                
                return RedirectToCurrentUmbracoPage();
            }
            else
            {
                AddModelErrors(result, "registerModel");
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

        private async Task SignInAsync(UmbracoApplicationMember member, bool isPersistent)
        {
            OwinContext.Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            OwinContext.Authentication.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent },
                await member.GenerateUserIdentityAsync(_userManager));
        }

        private void AddModelErrors(IdentityResult result, string prefix = "")
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(prefix, error);
            }
        }

        private bool HasPassword()
        {
            var user = _userManager.FindById(User.Identity.GetUserId<int>());
            if (user != null)
            {
                return !user.PasswordHash.IsNullOrWhiteSpace();
            }
            return false;
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

    }

}
