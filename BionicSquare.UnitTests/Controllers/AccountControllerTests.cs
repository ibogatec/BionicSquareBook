using BionicSquare.Models;
using BionicSquare.Models.ViewModels;
using BionicSquare.Utility;
using BionicSquareWeb.Areas.Identity.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BionicSquare.UnitTests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<IUrlHelper> _mockUrlHelper;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
            _mockUserManager.Object,
            httpContextAccessor.Object,
            userClaimsPrincipalFactory.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>());

        _controller = new AccountController(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockRoleManager.Object);

        _mockUrlHelper = new Mock<IUrlHelper>();
        _controller.Url = _mockUrlHelper.Object;
    }

    [Fact]
    public void LoginGet_ShouldSetReturnUrlInViewDataAndReturnView()
    {
        // Act
        var result = _controller.LoginGet(returnUrl: "/Customer/Home/Privacy");

        // Assert
        result.Should().BeOfType<ViewResult>();
        _controller.ViewData["ReturnUrl"].Should().Be("/Customer/Home/Privacy");
    }

    [Fact]
    public async Task LoginPostAsync_WhenModelStateIsInvalid_ShouldReturnViewWithSameModel()
    {
        // Arrange
        var model = new LoginViewModel { Email = "", Password = "" };
        _controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await _controller.LoginPostAsync(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _mockSignInManager.Verify(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task LoginPostAsync_WhenLoginSucceedsWithoutReturnUrl_ShouldRedirectToHomeIndex()
    {
        // Arrange
        var model = new LoginViewModel { Email = "user@test.com", Password = "Password123!", RememberMe = false };
        _mockSignInManager.Setup(s => s.PasswordSignInAsync("user@test.com", "Password123!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.LoginPostAsync(model, returnUrl: null);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
        redirect.RouteValues.Should().ContainKey("area").WhoseValue.Should().Be("Customer");
    }

    [Fact]
    public async Task LoginPostAsync_WhenLoginSucceedsWithLocalReturnUrl_ShouldRedirectToReturnUrl()
    {
        // Arrange
        const string returnUrl = "/Customer/Cart/Index";
        var model = new LoginViewModel { Email = "user@test.com", Password = "Password123!", RememberMe = true };
        _mockSignInManager.Setup(s => s.PasswordSignInAsync("user@test.com", "Password123!", true, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _mockUrlHelper.Setup(u => u.IsLocalUrl(returnUrl)).Returns(true);

        // Act
        var result = await _controller.LoginPostAsync(model, returnUrl: returnUrl);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be(returnUrl);
    }

    [Fact]
    public async Task LoginPostAsync_WhenLoginSucceedsWithNonLocalReturnUrl_ShouldRedirectToHomeIndex()
    {
        // Arrange
        const string returnUrl = "https://malicious.example.com";
        var model = new LoginViewModel { Email = "user@test.com", Password = "Password123!" };
        _mockSignInManager.Setup(s => s.PasswordSignInAsync("user@test.com", "Password123!", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _mockUrlHelper.Setup(u => u.IsLocalUrl(returnUrl)).Returns(false);

        // Act
        var result = await _controller.LoginPostAsync(model, returnUrl: returnUrl);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
    }

    [Fact]
    public async Task LoginPostAsync_WhenLoginFails_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var model = new LoginViewModel { Email = "wrong@test.com", Password = "BadPassword" };
        _mockSignInManager.Setup(s => s.PasswordSignInAsync("wrong@test.com", "BadPassword", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.LoginPostAsync(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage == "Invalid login attempt.");
    }

    [Fact]
    public async Task LoginPostAsync_WhenExceptionOccurs_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var model = new LoginViewModel { Email = "error@test.com", Password = "Pass" };
        _mockSignInManager.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Auth service down"));

        // Act
        var result = await _controller.LoginPostAsync(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Auth service down"));
    }

    [Fact]
    public async Task LogoutPost_ShouldSignOutAndRedirectToHome()
    {
        // Arrange
        _mockSignInManager.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.LogoutPost();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
        redirect.RouteValues.Should().ContainKey("area").WhoseValue.Should().Be("Customer");
        _mockSignInManager.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [Fact]
    public void RegisterGet_ShouldReturnViewWithRoleListAndReturnUrl()
    {
        // Act
        var result = _controller.RegisterGet(returnUrl: "/somewhere");

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        _controller.ViewData["ReturnUrl"].Should().Be("/somewhere");
        var model = viewResult.Model.Should().BeOfType<RegisterViewModel>().Subject;
        model.RoleList.Should().HaveCount(3);
        model.RoleList.Should().Contain(r => r.Value == Role.Customer);
        model.RoleList.Should().Contain(r => r.Value == Role.Admin);
        model.RoleList.Should().Contain(r => r.Value == Role.Employee);
    }

    [Fact]
    public async Task RegisterPostAsync_WhenModelStateIsInvalid_ShouldReturnViewWithSameModel()
    {
        // Arrange
        var model = new RegisterViewModel { Email = "" };
        _controller.ModelState.AddModelError("Email", "Required");

        // Act
        var result = await _controller.RegisterPostAsync(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterPostAsync_WhenRoleDoesNotExist_ShouldCreateRoleThenCreateUserAndRedirect()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "newuser@test.com",
            Password = "Password123!",
            Role = Role.Customer,
            Name = "New User",
            City = "City",
            State = "State",
            StreetAddress = "Street 1",
            PostalCode = "12345",
            PhoneNumber = "555-1234"
        };

        _mockRoleManager.Setup(r => r.RoleExistsAsync(Role.Customer)).ReturnsAsync(false);
        _mockRoleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), Role.Customer))
            .ReturnsAsync(IdentityResult.Success);
        _mockSignInManager.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RegisterPostAsync(model);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Index");
        redirect.ControllerName.Should().Be("Home");
        _mockRoleManager.Verify(r => r.CreateAsync(It.Is<IdentityRole>(ir => ir.Name == Role.Customer)), Times.Once);
        _mockUserManager.Verify(m => m.AddToRoleAsync(It.Is<ApplicationUser>(u => u.Email == "newuser@test.com"), Role.Customer), Times.Once);
        _mockSignInManager.Verify(s => s.SignInAsync(It.Is<ApplicationUser>(u => u.Email == "newuser@test.com"), false, null), Times.Once);
    }

    [Fact]
    public async Task RegisterPostAsync_WhenRoleExistsAndLocalReturnUrl_ShouldRedirectToReturnUrl()
    {
        // Arrange
        const string returnUrl = "/checkout";
        var model = new RegisterViewModel
        {
            Email = "shopper@test.com",
            Password = "Password123!",
            Role = Role.Customer,
            Name = "Shopper"
        };

        _mockRoleManager.Setup(r => r.RoleExistsAsync(Role.Customer)).ReturnsAsync(true);
        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), Role.Customer))
            .ReturnsAsync(IdentityResult.Success);
        _mockSignInManager.Setup(s => s.SignInAsync(It.IsAny<ApplicationUser>(), false, null))
            .Returns(Task.CompletedTask);
        _mockUrlHelper.Setup(u => u.IsLocalUrl(returnUrl)).Returns(true);

        // Act
        var result = await _controller.RegisterPostAsync(model, returnUrl: returnUrl);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be(returnUrl);
        _mockRoleManager.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
    }

    [Fact]
    public async Task RegisterPostAsync_WhenUserCreationFails_ShouldAddErrorsToModelStateAndReturnView()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "bad@test.com",
            Password = "Weak",
            Role = Role.Customer
        };

        _mockRoleManager.Setup(r => r.RoleExistsAsync(Role.Customer)).ReturnsAsync(true);
        var identityErrors = new[]
        {
            new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 6 characters." }
        };
        _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Weak"))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act
        var result = await _controller.RegisterPostAsync(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage == "Passwords must be at least 6 characters.");
    }

    [Fact]
    public async Task RegisterPostAsync_WhenExceptionOccurs_ShouldAddModelStateErrorAndReturnView()
    {
        // Arrange
        var model = new RegisterViewModel { Email = "fail@test.com", Role = Role.Customer };
        _mockRoleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failure"));

        // Act
        var result = await _controller.RegisterPostAsync(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(model);
        _controller.ModelState.IsValid.Should().BeFalse();
        _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("Database connection failure"));
    }

    [Fact]
    public void AccessDenied_ShouldReturnView()
    {
        // Act
        var result = _controller.AccessDenied();

        // Assert
        result.Should().BeOfType<ViewResult>();
    }
}
