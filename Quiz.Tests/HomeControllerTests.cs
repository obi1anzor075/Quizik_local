extern alias PL;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using DataAccessLayer.Repositories.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Moq;
using PresentationLayer.ErrorDescriber;
using Xunit;

namespace PresentationLayer.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<DataStoreDbContext> _contextMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<UrlEncoder> _urlEncoderMock;
        private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IQuizService> _quizServiceMock;
        private readonly Mock<IAdminTokenService> _adminTokenServiceMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IImageService> _imageServiceMock;

        public HomeControllerTests()
        {
            _contextMock = new Mock<DataStoreDbContext>();
            _userManagerMock = MockUserManager();
            _signInManagerMock = MockSignInManager(_userManagerMock.Object);
            _urlEncoderMock = new Mock<UrlEncoder>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _userServiceMock = new Mock<IUserService>();
            _authServiceMock = new Mock<IAuthService>();
            _quizServiceMock = new Mock<IQuizService>();
            _adminTokenServiceMock = new Mock<IAdminTokenService>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _imageServiceMock = new Mock<IImageService>();
        }

        #region Вспомогательные методы для создания моков

        private Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<SignInManager<User>> MockSignInManager(UserManager<User> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            return new Mock<SignInManager<User>>(
                userManager,
                contextAccessor.Object,
                claimsFactory.Object,
                null, null, null, null);
        }

        private PL.PresentationLayer.Controllers.HomeController CreateController()
        {
            return new PL.PresentationLayer.Controllers.HomeController(
                _contextMock.Object,
                _signInManagerMock.Object,
                _userManagerMock.Object,
                _urlEncoderMock.Object,
                _passwordHasherMock.Object, // Прокси мок для хешера
                _userServiceMock.Object,
                _authServiceMock.Object,
                _quizServiceMock.Object,
                _adminTokenServiceMock.Object,
                _userRepositoryMock.Object,
                _imageServiceMock.Object
            // Локализатор больше не передается, он не нужен
            );
        }

        #endregion

        #region Тесты для Login (GET)

        [Fact]
        public void Login_NotAuthenticated_ReturnsView()
        {
            // Arrange: создаем контроллер и симулируем неаутентифицированного пользователя.
            var controller = CreateController();
            var identity = new ClaimsIdentity();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = controller.Login();

            // Assert: ожидается, что вернется ViewResult
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Login_AuthenticatedUser_RedirectsToSelectMode()
        {
            // Arrange: создаем пользователя и настраиваем моки для аутентифицированного запроса.
            var user = new User
            {
                Id = "1",
                UserName = "test@example.com",
                Name = "TestUser",
                TwoFactorEnabled = false,
                ProfilePicture = new byte[0],
                ProfilePictureFileName = "profile.png"
            };

            _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(user);
            _signInManagerMock.Setup(x => x.IsSignedIn(It.IsAny<ClaimsPrincipal>()))
                .Returns(true);

            var controller = CreateController();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.UserName)
            }, "TestAuthType");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Act
            var result = controller.Login();

            // Assert: ожидается перенаправление на SelectMode
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SelectMode", redirectResult.ActionName);
        }

        #endregion

        #region Тесты для LoginAsync (POST)

        [Fact]
        public async Task LoginAsync_ValidCredentials_RedirectsToSelectMode()
        {
            // Arrange: симулируем корректные учетные данные
            var user = new User
            {
                Id = "1",
                UserName = "test@example.com",
                Name = "TestUser",
                TwoFactorEnabled = false,
            };

            var loginVM = new PL.PresentationLayer.ViewModels.LoginVM
            {
                Email = "test@example.com",
                Password = "Password123!",
                RememberMe = false
            };

            _userManagerMock.Setup(x => x.FindByNameAsync(loginVM.Email))
                .ReturnsAsync(user);

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(user, loginVM.Password, loginVM.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.LoginAsync(loginVM);

            // Assert: ожидается перенаправление на SelectMode
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SelectMode", redirectResult.ActionName);
        }

        [Fact]
        public async Task LoginAsync_InvalidCredentials_ReturnsViewWithErrors()
        {
            // Arrange: симулируем неверные учетные данные
            var loginVM = new PL.PresentationLayer.ViewModels.LoginVM
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword",
                RememberMe = false
            };

            _userManagerMock.Setup(x => x.FindByNameAsync(loginVM.Email))
                .ReturnsAsync((User)null);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.LoginAsync(loginVM);

            // Assert: проверяем, что ModelState содержит ошибки и возвращается ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.NotEmpty(controller.ModelState[string.Empty].Errors);
        }

        #endregion

        #region Тесты для Logout

        [Fact]
        public async Task Logout_Always_SignsOutAndRedirectsToLogin()
        {
            // Arrange: настраиваем мок для выхода
            _signInManagerMock.Setup(x => x.SignOutAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Logout();

            // Assert: проверяем, что вызвался метод выхода и было перенаправление на Login
            _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        #endregion

        #region Дополнительные тесты для Register, Verify2FA, Profile и других действий

        [Fact]
        public async Task Register_PostValidNonAdminUser_RedirectsToSelectMode()
        {
            // Arrange: симулируем корректную регистрацию нового пользователя без админского токена
            var registerVM = new PL.PresentationLayer.ViewModels.RegisterVM
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                UserName = "NewUser",
                Token = "nonadmin"
            };

            // Мок для получения случайного изображения профиля
            _imageServiceMock.Setup(x => x.GetRandomProfilePictureAsync())
                .ReturnsAsync((new byte[] { 1, 2, 3 }, "dummy.png"));

            // Мок для успешного создания пользователя
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), registerVM.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Мок для проверки токена админа (возвращаем false)
            _adminTokenServiceMock.Setup(x => x.ValidateTokenAsync(registerVM.Token))
                .ReturnsAsync(false);

            // Мок для добавления роли
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
                .ReturnsAsync(IdentityResult.Success);

            // Мок для входа пользователя
            _signInManagerMock.Setup(x => x.SignInAsync(It.IsAny<User>(), false, null))
                .Returns(Task.CompletedTask);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Register(registerVM);

            // Assert: ожидается перенаправление на SelectMode
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SelectMode", redirectResult.ActionName);
        }

        [Fact]
        public async Task Register_PostInvalidUser_ReturnsViewWithErrors()
        {
            // Arrange: симулируем ситуацию, когда создание пользователя завершается с ошибкой
            var registerVM = new PL.PresentationLayer.ViewModels.RegisterVM
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                UserName = "NewUser",
                Token = "nonadmin"
            };

            _imageServiceMock.Setup(x => x.GetRandomProfilePictureAsync())
                .ReturnsAsync((new byte[] { 1, 2, 3 }, "dummy.png"));

            var identityError = new IdentityError { Description = "Ошибка создания пользователя." };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), registerVM.Password))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Register(registerVM);

            // Assert: проверяем, что ModelState содержит ошибку и возвращается ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.NotEmpty(controller.ModelState[string.Empty].Errors);
        }

        [Fact]
        public async Task Verify2FA_PostValidCode_RedirectsToSelectMode()
        {
            // Arrange: тестируем успешную валидацию 2FA кода
            var verifyModel = new PL.PresentationLayer.Models.Verify2FAModel
            {
                UserId = "1",
                Code = "123456",
                RememberMe = false
            };

            var user = new User { Id = "1", UserName = "test@example.com" };

            _userManagerMock.Setup(x => x.FindByIdAsync(verifyModel.UserId))
                .ReturnsAsync(user);

            _signInManagerMock.Setup(x => x.TwoFactorAuthenticatorSignInAsync(verifyModel.Code, verifyModel.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Verify2FA(verifyModel);

            // Assert: ожидается перенаправление на SelectMode
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SelectMode", redirectResult.ActionName);
        }

        [Fact]
        public async Task Verify2FA_PostInvalidCode_ReturnsViewWithError()
        {
            // Arrange: тестируем ситуацию, когда введен неверный 2FA код
            var verifyModel = new PL.PresentationLayer.Models.Verify2FAModel
            {
                UserId = "1",
                Code = "000000",
                RememberMe = false
            };

            var user = new User { Id = "1", UserName = "test@example.com" };

            _userManagerMock.Setup(x => x.FindByIdAsync(verifyModel.UserId))
                .ReturnsAsync(user);

            _signInManagerMock.Setup(x => x.TwoFactorAuthenticatorSignInAsync(verifyModel.Code, verifyModel.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Verify2FA(verifyModel);

            // Assert: ожидается, что ModelState будет содержать ошибку, и возвращается ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.NotEmpty(controller.ModelState[string.Empty].Errors);
        }

        [Fact]
        public async Task VerifyCurrentPassword_ReturnsJsonResult()
        {
            // Arrange: тестируем метод проверки текущего пароля, где пароль корректен
            string testPassword = "test123";
            _userServiceMock.Setup(x => x.VerifyCurrentPasswordAsync(It.IsAny<ClaimsPrincipal>(), testPassword))
                .ReturnsAsync(true);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.VerifyCurrentPassword(testPassword);

            // Assert: ожидается JsonResult с полем success = true
            var jsonResult = Assert.IsType<JsonResult>(result);
            var resultObject = jsonResult.Value;
            var successProperty = resultObject.GetType().GetProperty("success");
            Assert.NotNull(successProperty);
            bool successValue = (bool)successProperty.GetValue(resultObject);
            Assert.True(successValue);

        }

        [Fact]
        public async Task ChangePassword_ValidPasswordChange_ReturnsSuccessJson()
        {
            // Arrange: тестируем изменение пароля, когда новый пароль удовлетворяет требованиям
            string currentPassword = "oldPass";
            string newPassword = "newPass";
            _userServiceMock.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), currentPassword, newPassword))
                .ReturnsAsync(true);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.ChangePassword(currentPassword, newPassword);

            // Assert: ожидается JsonResult с success = true и соответствующим сообщением
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            Assert.NotNull(data);

            // Проверяем success
            var successProperty = data.GetType().GetProperty("success");
            Assert.NotNull(successProperty);
            Assert.True((bool)successProperty.GetValue(data)); // <-- Должно быть True

            // Проверяем message
            var messageProperty = data.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Пароль успешно изменен.", messageProperty.GetValue(data)); // <-- Должно быть корректное сообщение

        }

        [Fact]
        public async Task ChangePassword_InvalidPasswordChange_ReturnsFailureJson()
        {
            // Arrange: тестируем изменение пароля, когда новый пароль не проходит проверку
            string currentPassword = "oldPass";
            string newPassword = "newPass";
            _userServiceMock.Setup(x => x.ChangePasswordAsync(It.IsAny<ClaimsPrincipal>(), currentPassword, newPassword))
                .ReturnsAsync(false);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.ChangePassword(currentPassword, newPassword);

            // Assert: ожидается JsonResult с success = false и соответствующим сообщением
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            Assert.NotNull(data);

            // Проверяем success
            var successProperty = data.GetType().GetProperty("success");
            Assert.NotNull(successProperty);
            Assert.False((bool)successProperty.GetValue(data));

            // Проверяем message
            var messageProperty = data.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Пароль не соответствует требованиям безопасности.", messageProperty.GetValue(data));


        }

        [Fact]
        public void SetLanguage_ValidCulture_ReturnsLocalRedirect()
        {
            // Arrange: тестируем установку языка
            string culture = "ru-RU";
            string returnUrl = "/home/index";

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.SetLanguage(culture, returnUrl);

            // Assert: ожидается локальное перенаправление на указанный URL
            var redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task Profile_UserNotFound_RedirectsToLogin()
        {
            // Arrange: симулируем ситуацию, когда пользователь не найден (пустой идентификатор)
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(string.Empty);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Profile();

            // Assert: ожидается перенаправление на страницу Login
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task Profile_UserFound_ReturnsViewWithUser()
        {
            // Arrange: симулируем ситуацию, когда пользователь найден и данные успешно загружены
            var user = new User
            {
                Id = "1",
                Name = "TestUser",
                Email = "test@example.com"
            };

            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(user.Id);
            _userManagerMock.Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Моки для получения изображения, QR-кода и секретного ключа
            _userServiceMock.Setup(x => x.GetProfilePictureBase64Async(user))
                .ReturnsAsync("base64Image");
            _userServiceMock.Setup(x => x.GenerateQRCodeForUserAsync(user))
                .ReturnsAsync("qrCodeUrl");
            _userServiceMock.Setup(x => x.GenerateSecretKeyForUserAsync(user))
                .ReturnsAsync("secretKey");
            _quizServiceMock.Setup(x => x.GetQuizResultsByUserAsync(user.Id))
                .ReturnsAsync(new List<QuizResult>());

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.Profile();

            // Assert: проверяем, что возвращается ViewResult с моделью пользователя
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(user, viewResult.Model);
        }

        [Fact]
        public async Task UpdateProfile_ValidUser_UpdatesAndRedirects()
        {
            // Arrange: симулируем обновление профиля существующего пользователя
            var user = new User
            {
                Id = "1",
                Name = "OldName",
                Email = "old@example.com",
                UserName = "old@example.com"
            };

            _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            var updatedUser = new User
            {
                Name = "NewName",
                Email = "new@example.com"
            };

            // Моки для обработки аватара
            byte[] processedImage = new byte[] { 1, 2, 3 };
            _imageServiceMock.Setup(x => x.ProcessImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(processedImage);
            _imageServiceMock.Setup(x => x.ResizeImageIfNecessaryAsync(processedImage, 150, 150))
                .ReturnsAsync(processedImage);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.UpdateProfile(updatedUser, null);

            // Assert: проверяем, что метод обновления пользователя вызван и происходит перенаправление на Profile
            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
        }

        [Fact]
        public async Task GoogleResponse_NoExternalLoginInfo_RedirectsToLogin()
        {
            // Arrange: симулируем ситуацию, когда внешняя аутентификация не возвращает информацию
            _signInManagerMock.Setup(x => x.SignInAsync(It.IsAny<User>(), false, null))
                .Returns(Task.CompletedTask);

            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await controller.GoogleResponse();

            // Assert: ожидается перенаправление на Login
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        #endregion
    }
}
