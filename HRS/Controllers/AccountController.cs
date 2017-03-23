using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using HRS.Models;
using HRS.ViewModels;
using HRS.DAL;
using System.Net;
using System.Data.Entity;
using System.Collections.Generic;
using System;
using System.Linq;

namespace HRS.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public AccountController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new UserDbContext())))
        {
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; private set; }
        
        private UserDbContext db = new UserDbContext();
        private RoleManager<IdentityRole> roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(new UserDbContext()));

        //
        // GET: /Account
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var users = db.Users
                .OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.UserName)
                .ToList();

            return View(users);
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindAsync(model.UserName, model.Password);
                if (user != null && user.IsActive)
                {
                    await SignInAsync(user, false);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [Authorize(Roles = "Admin")]
        public ActionResult Register()
        {
            ViewBag.AllRoles = new SelectList(db.Roles.ToList(), "Name", "Name");

            var model = new RegisterViewModel();
            model.Roles = new List<string>();
            model.IsActive = true;

            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() 
                { 
                    UserName = model.UserName,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Phone = model.Phone,
                    Email = model.Email,
                    IsActive = model.IsActive
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var resultRole = new IdentityResult();
                    // add user roles
                    if (model.Roles != null)
                    {
                        
                        foreach (var role in model.Roles)
                        {
                            if (roleManager.RoleExists(role))
                            {
                                resultRole = UserManager.AddToRole(user.Id, role);
                                if (!resultRole.Succeeded)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (resultRole == null || resultRole.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        AddErrors(resultRole);
                    }
                }
                else
                {
                    AddErrors(result);
                }
            }
            
            // If we got this far, something failed, redisplay form
            ViewBag.AllRoles = new SelectList(db.Roles.ToList(), "Name", "Name");
            
            return View(model);
        }

        //
        // GET: /Account/Edit/achera
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Edit(string id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }
 
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == id);
 
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var model = new EditViewModel()
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                Email = user.Email,
                IsActive = user.IsActive
            };
            model.Roles = new List<string>();
            model.Roles = user.Roles.Select(r => r.Role.Name).ToList();

            ViewBag.AllRoles = new SelectList(db.Roles.ToList(), "Name", "Name");

            return View(model);
        }

        //
        // POST: /Account/Edit/achera
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(EditViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await db.Users.FirstAsync(u => u.UserName == model.UserName);
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Phone = model.Phone;
                    user.Email = model.Email;
                    user.IsActive = model.IsActive;

                    db.Entry(user).State = EntityState.Modified;
                    await db.SaveChangesAsync();

                    // delete revoked user roles
                    var userRoles = user.Roles.Select(r => r.Role.Name).ToList();
                    var result = new IdentityResult();

                    if (userRoles != null)
                    {
                        foreach (var role in userRoles)
                        {
                            if (model.Roles == null || !model.Roles.Contains(role))
                            {
                                result = UserManager.RemoveFromRole(user.Id, role);
                                if (!result.Succeeded)
                                {
                                    AddErrors(result);
                                    throw (new Exception());
                                }
                            }
                        }
                    }
                    // add new user roles
                    if (model.Roles != null)
                    {
                        foreach (var role in model.Roles)
                        {
                            if (roleManager.RoleExists(role) && !userRoles.Contains(role))
                            {
                                result = UserManager.AddToRole(user.Id, role);
                                if (!result.Succeeded)
                                {
                                    AddErrors(result);
                                    throw (new Exception());
                                }
                            }
                        }
                    }

                    return RedirectToAction("Index");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Error saving");
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.AllRoles = new SelectList(db.Roles.ToList(), "Name", "Name");

            return View(model);
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Login", "Account");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
                roleManager.Dispose();

                if (UserManager != null)
                {
                    UserManager.Dispose();
                    UserManager = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }
}