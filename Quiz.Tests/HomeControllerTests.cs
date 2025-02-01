using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using PresentationLayer.Controllers;
using PresentationLayer.ViewModels;
using Microsoft.AspNetCore.Identity;
using DataAccessLayer.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using PresentationLayer.ErrorDescriber;

public class HomeControllerTests
{
    private readonly Mock<SignInManager<User>> _mockSignInManager;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IPasswordHasher<User>> _mockPasswordHasher;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockUserManager = new Mock<UserManager<User>>(
            new Mock<IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null
        );

        _mockSignInManager = new Mock<SignInManager<User>>(
            _mockUserManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<User>>().Object,
            null, null, null, null
        );

        _mockPasswordHasher = new Mock<IPasswordHasher<User>>();

        _controller = new HomeController(
            _mockSignInManager.Object,
            _mockUserManager.Object,
            new LocalizedIdentityErrorDescriber(),
            _mockPasswordHasher.Object
        );
    }

    [Fact]
    public async Task LoginAsync_ValidUser_ReturnsRedirectToAction()
    {
        // Arrange
        var loginVM = new LoginVM { Email = "test@example.com", Password = "password", RememberMe = false };

        var user = new User { UserName = loginVM.Email, Name = "Test User" };

        _mockUserManager.Setup(um => um.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.LoginAsync(loginVM);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("SelectMode", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }

    [Fact]
    public async Task LoginAsync_InvalidUser_ReturnsViewWithModel()
    {
        // Arrange
        var loginVM = new LoginVM { Email = "test@example.com", Password = "wrongpassword", RememberMe = false };

        _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.LoginAsync(loginVM);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(loginVM, viewResult.Model);
    }

[Fact]
public async Task Register_ValidUser_ReturnsRedirectToAction()
{
    // Arrange
    var registerVM = new RegisterVM { Email = "test@example.com", Password = "Password123!", UserName = "TestUser" };

    _mockUserManager.Setup(um => um.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
        .ReturnsAsync(IdentityResult.Success);
    _mockSignInManager.Setup(sm => sm.SignInAsync(It.IsAny<User>(), false, null))
        .Returns(Task.CompletedTask);

    // Act
    var result = await _controller.Register(registerVM);

    // Assert
    var redirectResult = Assert.IsType<RedirectToActionResult>(result);
    Assert.Equal("SelectMode", redirectResult.ActionName);
    Assert.Equal("Home", redirectResult.ControllerName);
}


    [Fact]
    public async Task Logout_RedirectsToLogin()
    {
        // Arrange
        _mockSignInManager.Setup(sm => sm.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirectResult.ActionName);
        Assert.Equal("Home", redirectResult.ControllerName);
    }
}
