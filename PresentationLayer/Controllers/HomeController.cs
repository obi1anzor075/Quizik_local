using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;
using System.Diagnostics;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PresentationLayer.ViewModels;
using Microsoft.AspNetCore.Identity;
using PresentationLayer.ErrorDescriber;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using PresentationLayer.Utilities;

namespace PresentationLayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly LocalizedIdentityErrorDescriber _localizedIdentityErrorDescriber;
        private readonly IPasswordHasher<User> _passwordHasher;

        private readonly SharedViewLocalizer _localizer;

        public HomeController(SignInManager<User> signInManager, UserManager<User> userManager, LocalizedIdentityErrorDescriber localizedIdentityErrorDescriber, IPasswordHasher<User> passwordHasher, SharedViewLocalizer localizer)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _localizedIdentityErrorDescriber = localizedIdentityErrorDescriber;
            _passwordHasher = passwordHasher;
            _localizer = localizer;
        }

        public IActionResult Login()
        {            
            
            // �����������
            var localizedStrings = _localizer.GetAllLocalizedStrings("Login");

            // �������� ����� � ViewData
            ViewData["LocalizedStrings"] = localizedStrings;
            if (User.Identity.IsAuthenticated)
            {
                var user = _userManager.FindByNameAsync(User.Identity.Name).Result;
                if (user != null)
                {
                    SaveUserNameInCookie(user.Name);
                    return RedirectToAction("SelectMode");
                }
            }



            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync(LoginVM model)
        {
            // �����������
            var localizedStrings = _localizer.GetAllLocalizedStrings("Login");

            // �������� ����� � ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email!, model.Password!, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(model.Email!);
                    return RedirectToAction("SelectMode", "Home");
                }
                ModelState.AddModelError(string.Empty, _localizedIdentityErrorDescriber.InvalidLogin().Description);
                return View(model);
            }

            return View(model);
        }

        public IActionResult Register()
        {
            // �����������
            var localizedStrings = _localizer.GetAllLocalizedStrings("Register");

            // �������� ����� � ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {            
            
            // �����������
            var localizedStrings = _localizer.GetAllLocalizedStrings("Register");

            // �������� ����� � ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            if (ModelState.IsValid)
            {
                User user = new()
                {
                    UserName = model.Email,
                    Name = model.UserName,
                    Email = model.Email,
                    CreatedAt = DateTime.Now,
                };
                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, false);
                    return RedirectToAction("SelectMode", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

        [Authorize]
        public IActionResult SelectMode()
        {
            // �����������
            var localizedStrings = _localizer.GetAllLocalizedStrings("SelectMode");

            // �������� ����� � ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            return View();
        }

        // Delimetrer string
        static string GetFirstWord(string input)
        {
            // ��������� ������ �� �������� � ����� ������ �����
            string[] words = input.Split(' ');
            if (words.Length > 0)
            {
                return words[0];
            }
            return string.Empty;
        }

        [AllowAnonymous]
        public IActionResult LoginGoogle()
        {
            string redirectUrl = Url.Action("GoogleResponse", "Home");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            // �����������
            var localizedStrings = _localizer.GetAllLocalizedStrings("SelectMode");

            // �������� ����� � ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                string userName = info.Principal.FindFirst(ClaimTypes.Name)?.Value.Split(' ')[0];
                SaveUserNameInCookie(userName);
                return RedirectToAction("SelectMode");
            }

            var email = info.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var googleId = info.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = info.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var fakePassword = "C0mpl3xP@ssw0rd!";

                user = new User
                {
                    GoogleId = googleId,
                    Email = email,
                    UserName = email,
                    Name = name.Split(' ')[0],
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                // Manually hash the placeholder password and set the PasswordHash property
                user.PasswordHash = _passwordHasher.HashPassword(user, fakePassword);

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }

                createResult = await _userManager.AddLoginAsync(user, info);
                if (!createResult.Succeeded)
                {
                    return RedirectToAction(nameof(Login));
                }
            }
            else
            {
                user.GoogleId = googleId;
                user.Name = name.Split(' ')[0]; // Assume first name only
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            SaveUserNameInCookie(user.Name);

            return RedirectToAction("SelectMode");
        }


        private void SaveUserNameInCookie(string userName)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            if (HttpContext.Request.Cookies.ContainsKey("UserName"))
            {
                HttpContext.Response.Cookies.Delete("UserName");
            }

            HttpContext.Response.Cookies.Append("UserName", userName, cookieOptions);
        }



        [HttpGet]
        public IActionResult GetUserName()
        {
            if (HttpContext.Request.Cookies.TryGetValue("userName", out var userName))
            {
                return Json(new { userName });
            }
            return Json(new { userName = "" });
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }

            return LocalRedirect(returnUrl);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}