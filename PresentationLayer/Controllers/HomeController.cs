using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;
using System.Diagnostics;
using DataAccessLayer.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PresentationLayer.ViewModels;
using Microsoft.AspNetCore.Identity;
using PresentationLayer.ErrorDescriber;
using Microsoft.AspNetCore.Localization;
using PresentationLayer.Utilities;
using Microsoft.AspNetCore.StaticFiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.WebUtilities;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using OtpNet;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity.UI.V4.Pages.Account.Manage.Internal;
using DataAccessLayer.DataContext;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;


namespace PresentationLayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly UrlEncoder _urlEncoder;
        private readonly IPasswordHasher<User> _passwordHasher;

        //private readonly SharedViewLocalizer _localizer;

        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly IQuizService _quizService;
        private readonly IAdminTokenService _adminTokenService;

        private readonly IUserRepository _userRepository;
        private readonly IImageService _imageService;

        public HomeController(SignInManager<User> signInManager, UserManager<User> userManager, UrlEncoder urlEncoder, IPasswordHasher<User> passwordHasher, IUserService userService,IAuthService authService,IQuizService quizService, IAdminTokenService adminTokenService, IUserRepository userRepository, IImageService imageService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _urlEncoder = urlEncoder;
            _passwordHasher = passwordHasher;
            //_localizer = localizer;
            _userService = userService;
            _authService = authService;
            _quizService = quizService;
            _adminTokenService = adminTokenService;
            _userRepository = userRepository;
            _imageService = imageService;
        }

        public IActionResult Login()
        {
            //var localizedStrings = _localizer.GetAllLocalizedStrings("Login");
            //ViewData["LocalizedStrings"] = localizedStrings;

            if (User.Identity.IsAuthenticated)
            {
                var user = _userManager.FindByNameAsync(User.Identity.Name).Result;
                if (user != null)
                {
                    if (user.TwoFactorEnabled && !_signInManager.IsSignedIn(User))
                    {
                        return RedirectToAction("Verify2FA", new { userId = user.Id });
                    }

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
            //var localizedStrings = _localizer.GetAllLocalizedStrings("Login");
            //ViewData["LocalizedStrings"] = localizedStrings;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Email!);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "�������� ��� ������������ ��� ������.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(user, model.Password!, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    return RedirectToAction("SelectMode", "Home");
                }
                else if (result.RequiresTwoFactor)
                {
                    // ��������������� �� �������� 2FA
                    return RedirectToAction("Verify2FA", "Home", new { userId = ((IdentityUser)user).Id, rememberMe = model.RememberMe });
                }

                ModelState.AddModelError(string.Empty, "�������� ��� ������������ ��� ������.");
            }

            return View(model);
        }

        public IActionResult Register()
        {
            // �����������
            //var localizedStrings = _localizer.GetAllLocalizedStrings("Register");

            //// �������� ����� � ViewData
            //ViewData["LocalizedStrings"] = localizedStrings;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            // �����������
            //var localizedStrings = _localizer.GetAllLocalizedStrings("Register");
            //ViewData["LocalizedStrings"] = localizedStrings;

            var token = model.Token;

            if (ModelState.IsValid)
            {
                // ������������������� ������ � ������� ����������� � ��� ������
                var (imageData, fileName) = await _imageService.GetRandomProfilePictureAsync();

                User user = new()
                {
                    UserName = model.Email,
                    Name = model.UserName,
                    Email = model.Email,
                    CreatedAt = DateTime.Now,
                    ProfilePicture = imageData,
                    ProfilePictureFileName = fileName // ��������� ��� �����
                };

                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    bool isAdmin = await _adminTokenService.ValidateTokenAsync(token);
                    SaveUserNameInCookie(model.UserName!);

                    if (isAdmin)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                        await _adminTokenService.InvalidateTokenAsync(token);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    await _signInManager.SignInAsync(user,isPersistent: false);
                    return RedirectToAction("SelectMode", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult LoginGoogle()
        {
            string redirectUrl = Url.Action("GoogleResponse", "Home");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> VerifyCurrentPassword(string currentPassword)
        {
            var isValid = await _userService.VerifyCurrentPasswordAsync(User, currentPassword);

            return Json(new {success = isValid});
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
        {
            var isPasswordChanged = await _userService.ChangePasswordAsync(User, currentPassword, newPassword);

            if (isPasswordChanged)
            {
                return Json(new { success = true, message = "������ ������� �������." });
            }
            else
            {
                return Json(new { success = false, message = "������ �� ������������� ����������� ������������." });
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Verify2FA(string userId, bool rememberMe)
        {
            return View(new Verify2FAModel { UserId = userId, RememberMe = rememberMe });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Verify2FA(Verify2FAModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(model.Code, model.RememberMe, rememberClient: false);
            if (result.Succeeded)
            {
                return RedirectToAction("SelectMode", "Home");
            }

            ModelState.AddModelError(string.Empty, "�������� ��� �������������.");
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> SelectMode()
        {
            //// �����������
            //var localizedStrings = _localizer.GetAllLocalizedStrings("SelectMode");

            //// �������� ����� � ViewData
            //ViewData["LocalizedStrings"] = localizedStrings;

            await ClearCookies();
            
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            string base64ProfilePicture = string.Empty;

            if (user.ProfilePicture != null && user.ProfilePicture.Length > 0)
            {
                // ����������� MIME-���� �� ������ ���������� �����
                var provider = new FileExtensionContentTypeProvider();
                string contentType = "application/octet-stream"; // ��� �� ���������

                if (provider.TryGetContentType(user.ProfilePictureFileName, out string detectedContentType))
                {
                    contentType = detectedContentType;
                }

                base64ProfilePicture = $"data:{contentType};base64,{Convert.ToBase64String(user.ProfilePicture)}";
            }

            ViewBag.ProfilePicture = base64ProfilePicture;

            return View();
        }

        // �������� �������� ������������������
        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        // �������� �������� ������������� Cookies
        public IActionResult CookiesPolicy()
        {
            return View();
        }

        // �������� ������� �������������
        public IActionResult TermsofUse()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Login");

            ViewBag.Name = user.Name;
            ViewBag.Email = user.Email;

            // �������� ����������� ������� � ������� Base64
            ViewBag.ProfilePicture = await _userService.GetProfilePictureBase64Async(user);

            // ��������� QR-���� ��� 2FA
            ViewBag.QrCodeImageUrl = await _userService.GenerateQRCodeForUserAsync(user);

            // ��������� ���������� �����
            ViewBag.SecretKey = await _userService.GenerateSecretKeyForUserAsync(user);

            // �������� ���������� ��������
            ViewBag.QuizResults = await _quizService.GetQuizResultsByUserAsync(user.Id);

            return View(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(User updatedUser, IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.Email;
            user.NormalizedEmail = updatedUser.Email.ToUpper();

            if (avatar != null && avatar.Length > 0)
            {
                user.ProfilePictureFileName = avatar.FileName;

                var imageBytes = await _imageService.ProcessImageAsync(avatar);

                user.ProfilePicture = await _imageService.ResizeImageIfNecessaryAsync(imageBytes, maxWidth: 150, maxHeight: 150);
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Profile");
        }

        //���������� 2FA
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Disable2FA()
        {
            var result = await _authService.Disable2FAAsync(User);
            if (result is OkObjectResult okResult)
            {
                return Json(new { success = true, message = "������������� �������������� ���������." });
            }
            else if (result is BadRequestObjectResult badRequestResult)
            {
                // ��������� ������
                return Json(new { success = false, message = "������ ��� ���������� 2FA." });
            }
            return View("Error", new ErrorViewModel { ErrorMessage = "Unknown error" });
        }

        // ��������� ���� �� Google Authenticator
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Verify2FACode(string code)
        {
            var user = await _userManager.GetUserAsync(User);

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, "Authenticator", code);
            if (isValid)
            {
                user.TwoFactorEnabled = true;
                await _userManager.UpdateAsync(user);
                return Json(new { success = true, message = "2FA ������� ������������!" });
            }

            return Json(new { success = false, message = "�������� ���. ��������� �������." });
        }

        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            //// �����������
            //var localizedStrings = _localizer.GetAllLocalizedStrings("SelectMode");

            //// �������� ����� � ViewData
            //ViewData["LocalizedStrings"] = localizedStrings;

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


        [Authorize]
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

        // ������� ����
        private async Task ClearCookies()
        {
            // ������� ���� � ������ "CurrentQuestionIndex"
            Response.Cookies.Delete("CurrentQuestionIndex");

            // ���� ��������� ����������� ���������, ����� ������� ���������� ������
            await Task.CompletedTask;
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetUserName()
        {
            if (HttpContext.Request.Cookies.TryGetValue("userName", out var userName))
            {
                return Json(new { userName });
            }
            return Json(new { userName = "" });
        }

        [HttpPost]
        [Authorize]
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