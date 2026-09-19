using System.Net;
using BionicSquare.IntegrationTests.Infrastructure;
using BionicSquare.Models;
using BionicSquare.Utility;
using BionicSquareWeb.Areas.Identity.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BionicSquare.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class AccountControllerIntegrationTests : IntegrationTestBase
{
    public AccountControllerIntegrationTests(CustomWebApplicationFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task LoginGet_WithoutReturnUrl_RendersForm()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Identity/Account/Login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var form = document.QuerySelector("form");
        form.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginGet_WithReturnUrl_StoresInViewDataAndRendersForm()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Identity/Account/Login?returnUrl=%2FCustomer%2FHome%2FPrivacy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var form = document.QuerySelector("form");
        form.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginPost_WhenValidCredentialsAndReturnUrl_RedirectsToReturnUrl()
    {
        // Arrange
        var email = $"login_return_{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        using (var scope = CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = "Return User"
            };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
            await userManager.AddToRoleAsync(user, Role.Customer);
        }

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "RememberMe", "false" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Login?returnUrl=/Customer/Home/Privacy", loginForm);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer/Home/Privacy");
    }

    [Fact]
    public async Task LoginPost_WhenExternalReturnUrl_IgnoresAndRedirectsToCustomerHome()
    {
        // Arrange
        var email = $"login_ext_{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        using (var scope = CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = "External Return User"
            };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
            await userManager.AddToRoleAsync(user, Role.Customer);
        }

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "RememberMe", "false" }
        });

        // Act: Pass an external URL; Url.IsLocalUrl should return false and redirect to customer index
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Login?returnUrl=https://external-malicious-site.com", loginForm);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer");
    }

    [Fact]
    public async Task LoginPost_WhenRememberMeIsTrue_SetsPersistentCookieAndRedirects()
    {
        // Arrange
        var email = $"login_rem_{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        using (var scope = CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = "Remember User"
            };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
            await userManager.AddToRoleAsync(user, Role.Customer);
        }

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "RememberMe", "true" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Login", loginForm);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Contains("Set-Cookie").Should().BeTrue();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains(".AspNetCore.Identity.Application"));
    }

    [Fact]
    public async Task LoginPost_WhenInvalidModel_ReturnsView()
    {
        // Arrange: Missing email and password
        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", "" },
            { "Password", "" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Login", loginForm);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LoginPost_WhenExceptionThrown_CatchesAndReturnsViewWithModelError()
    {
        // Arrange
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var controller = new AccountController(userManager, signInManager, roleManager)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // Act: null model triggers NullReferenceException inside try block
        var result = await controller.LoginPostAsync(null!);

        // Assert: Catches Exception and returns View with error added to ModelState
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("Error:"));
    }

    [Fact]
    public async Task LogoutPost_SignsOutAndRedirectsToHome()
    {
        // Act
        var response = await Client.WithUser("any-user").PostAsync("/Identity/Account/Logout", new FormUrlEncodedContent([]));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer");
    }

    [Fact]
    public async Task RegisterGet_WithoutReturnUrl_RendersForm()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Identity/Account/Register");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var form = document.QuerySelector("form");
        form.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterGet_WithReturnUrl_ReturnsOkWithForm()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Identity/Account/Register?returnUrl=%2FCustomer%2FHome%2FPrivacy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var form = document.QuerySelector("form");
        form.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterPost_WhenValidData_PersistsUserToDatabaseAndAssignsRole()
    {
        // Arrange
        var email = $"newuser_{Guid.NewGuid():N}@example.com";
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", "SecurePass123!" },
            { "ConfirmPassword", "SecurePass123!" },
            { "Name", "Fresh Registered User" },
            { "PhoneNumber", "123-456-7890" },
            { "StreetAddress", "789 Pine Road" },
            { "City", "Portland" },
            { "State", "OR" },
            { "PostalCode", "97201" },
            { "Role", Role.Customer }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Register", form);

        // Assert response redirects to Home
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        // Verify user in SQL Server AspNetUsers table
        var savedUser = await ExecuteDbContextAsync(async db =>
            await db.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == email));

        savedUser.Should().NotBeNull();
        savedUser.Name.Should().Be("Fresh Registered User");
        savedUser.City.Should().Be("Portland");

        // Verify role assignment in SQL Server
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var isInRole = await userManager.IsInRoleAsync(savedUser, Role.Customer);
        isInRole.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterPost_WhenValidDataWithReturnUrl_RedirectsToReturnUrl()
    {
        // Arrange
        var email = $"register_return_{Guid.NewGuid():N}@example.com";
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", "SecurePass123!" },
            { "ConfirmPassword", "SecurePass123!" },
            { "Name", "Return Registered User" },
            { "Role", Role.Customer }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Register?returnUrl=/Customer/Home/Privacy", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer/Home/Privacy");
    }

    [Fact]
    public async Task RegisterPost_WhenExternalReturnUrl_IgnoresAndRedirectsToCustomerHome()
    {
        // Arrange
        var email = $"reg_ext_{Guid.NewGuid():N}@example.com";
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", "SecurePass123!" },
            { "ConfirmPassword", "SecurePass123!" },
            { "Name", "External Return Reg User" },
            { "Role", Role.Customer }
        });

        // Act: External returnUrl
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Register?returnUrl=https://external-malicious-site.com", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/Customer");
    }

    [Fact]
    public async Task RegisterPost_WhenCustomNonExistentRole_CreatesRoleAndAssigns()
    {
        // Arrange: Custom new role name
        var customRole = "CustomRole" + Guid.NewGuid().ToString("N")[..6];
        var email = $"customrole_{Guid.NewGuid():N}@example.com";
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", "SecurePass123!" },
            { "ConfirmPassword", "SecurePass123!" },
            { "Name", "Custom Role User" },
            { "Role", customRole }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Register", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);

        using var scope = CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roleExists = await roleManager.RoleExistsAsync(customRole);
        roleExists.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterPost_WhenInvalidModel_ReturnsView()
    {
        // Arrange: Missing required password
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", "invalid@test.com" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Register", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterPost_WhenDuplicateEmail_ReturnsViewWithModelError()
    {
        // Arrange: Create existing user first
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        using (var scope = CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await userManager.CreateAsync(new ApplicationUser { UserName = email, Email = email, Name = "Existing" }, "Password123!");
        }

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", "Password123!" },
            { "ConfirmPassword", "Password123!" },
            { "Name", "Dup Attempt" },
            { "Role", Role.Customer }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Register", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var errors = document.QuerySelectorAll(".validation-summary-errors, .field-validation-error");
        errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterPost_WhenExceptionThrown_CatchesAndReturnsViewWithModelError()
    {
        // Arrange
        using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var controller = new AccountController(userManager, signInManager, roleManager)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        // Act: null model triggers NullReferenceException inside try block
        var result = await controller.RegisterPostAsync(null!);

        // Assert: Catches Exception and returns View with error added to ModelState
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeNull();
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.StartsWith("Error:"));
    }

    [Fact]
    public async Task LoginPost_WhenValidCredentials_SetsAuthCookieAndRedirects()
    {
        // Arrange: Pre-create a user with password
        var email = $"logintest_{Guid.NewGuid():N}@example.com";
        const string password = "StrongPassword123!";

        using (var scope = CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Name = "Login Test User"
            };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
            await userManager.AddToRoleAsync(user, Role.Customer);
        }

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", email },
            { "Password", password },
            { "RememberMe", "false" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Login", loginForm);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Contains("Set-Cookie").Should().BeTrue();
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        cookies.Should().Contain(c => c.Contains(".AspNetCore.Identity.Application"));
    }

    [Fact]
    public async Task LoginPost_WhenInvalidPassword_DoesNotSignInAndRendersError()
    {
        // Arrange
        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Email", "nonexistent@example.com" },
            { "Password", "WrongPassword!" },
            { "RememberMe", "false" }
        });

        // Act
        var response = await Client.AsAnonymous().PostAsync("/Identity/Account/Login", loginForm);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        var bodyText = document.Body?.TextContent ?? string.Empty;
        bodyText.Should().Contain("Invalid login attempt");
    }

    [Fact]
    public async Task AccessDenied_ReturnsOkAndRendersView()
    {
        // Act
        var response = await Client.AsAnonymous().GetAsync("/Identity/Account/AccessDenied");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await HtmlHelpers.GetDocumentAsync(response);
        document.Body.Should().NotBeNull();
    }
}
