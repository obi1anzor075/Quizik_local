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
            
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Login");

            // Передача строк в ViewData
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
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Login");

            // Передача строк в ViewData
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
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Register");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("Register");
            ViewData["LocalizedStrings"] = localizedStrings;

            if (ModelState.IsValid)
            {
                // Деструктуризованный кортеж с данными изобрадения и его именем
                var (imageData, fileName) = GetRandomProfilePicture();

                User user = new()
                {
                    UserName = model.Email,
                    Name = model.UserName,
                    Email = model.Email,
                    CreatedAt = DateTime.Now,
                    ProfilePicture = imageData,
                    ProfilePictureFileName = fileName // Сохраняем имя файла
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


        [AllowAnonymous]
        public IActionResult LoginGoogle()
        {
            string redirectUrl = Url.Action("GoogleResponse", "Home");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }

        [Authorize]
        public IActionResult SelectMode()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("SelectMode");

            // Передача строк в ViewData
            ViewData["LocalizedStrings"] = localizedStrings;

            return View();
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            string base64ProfilePicture = string.Empty;

            if (user.ProfilePicture != null && user.ProfilePicture.Length > 0)
            {
                // Определение MIME-типа на основе расширения файла
                var provider = new FileExtensionContentTypeProvider();
                string contentType = "application/octet-stream"; // Тип по умолчанию

                if (provider.TryGetContentType(user.ProfilePictureFileName, out string detectedContentType))
                {
                    contentType = detectedContentType;
                }

                base64ProfilePicture = $"data:{contentType};base64,{Convert.ToBase64String(user.ProfilePicture)}";
            }

            ViewBag.ProfilePicture = base64ProfilePicture;
            ViewBag.Name = user.Name;
            ViewBag.Email = user.Email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(User updatedUser, IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.UserName = updatedUser.Email;
            user.NormalizedEmail = updatedUser.Email.ToUpper();
            user.ProfilePictureFileName = avatar.FileName;

            if (avatar != null && avatar.Length > 0)
            {
                using MemoryStream ms = new MemoryStream();
                await avatar.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                var resizedImage = ResizeImageIfNecessary(imageBytes, maxWidth: 150, maxHeight: 150);

                user.ProfilePicture = resizedImage;
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction("Profile");
        }

        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            // Локализация
            var localizedStrings = _localizer.GetAllLocalizedStrings("SelectMode");

            // Передача строк в ViewData
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

        // Delimetrer string
        static string GetFirstWord(string input)
        {
            // Разделяем строку по пробелам и берем первое слово
            string[] words = input.Split(' ');
            if (words.Length > 0)
            {
                return words[0];
            }
            return string.Empty;
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

        private (byte[] ImageData, string FileName) GetRandomProfilePicture()
        {
            var imageFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/default-profile-pictures");
            var images = Directory.GetFiles(imageFolder);
            if (images.Length == 0)
                return (null, null);

            var random = new Random();
            var randomImagePath = images[random.Next(images.Length)];
            var imageData = System.IO.File.ReadAllBytes(randomImagePath);
            var fileName = Path.GetFileName(randomImagePath); // Извлекаем только имя файла

            return (imageData, fileName);
        }

        private byte[] ResizeImageIfNecessary(byte[] imageBytes, int maxWidth, int maxHeight)
        {
            using var imageWithFormat = Image.Load(imageBytes);
            var image = imageWithFormat;//получение самого изображения
            var format = imageWithFormat.Metadata.DecodedImageFormat; //получение формата

            if (image.Width > maxWidth || image.Height > maxHeight)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max, //сохранине пропорций
                    Size = new Size(maxWidth, maxHeight)
                }));
            }

            //сохранение изобрадения в том же формате
            using MemoryStream ms = new MemoryStream();
            image.Save(ms, format);
            return ms.ToArray();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}