using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BionicSquare.IntegrationTests.Infrastructure;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue(UserIdHeader, out var userIdValues))
        {
            var userId = userIdValues.ToString();
            var claims = new List<Claim>();

            if (userId != "__NO_NAME_ID__")
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }
            claims.Add(new Claim(ClaimTypes.Name, userId));

            if (Request.Headers.TryGetValue(RoleHeader, out var roleValues))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleValues.ToString()));
            }

            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return AuthenticateResult.Success(ticket);
        }

        // Check if cookie authentication is present (e.g. from real login endpoint)
        var cookieResult = await Context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        return cookieResult.Succeeded ? cookieResult : AuthenticateResult.NoResult();
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var returnUrl = Request.Path + Request.QueryString;
        var redirectUri = $"/Identity/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        Response.StatusCode = StatusCodes.Status302Found;
        Response.Headers.Location = redirectUri;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status302Found;
        Response.Headers.Location = "/Identity/Account/AccessDenied";
        return Task.CompletedTask;
    }
}
