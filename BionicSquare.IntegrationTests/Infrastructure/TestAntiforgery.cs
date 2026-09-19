using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace BionicSquare.IntegrationTests.Infrastructure;

public class TestAntiforgery : IAntiforgery
{
    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
    {
        return new AntiforgeryTokenSet("test-request-token", "test-cookie-token", "__RequestVerificationToken", "test-header");
    }

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
    {
        return new AntiforgeryTokenSet("test-request-token", "test-cookie-token", "__RequestVerificationToken", "test-header");
    }

    public Task<bool> IsRequestValidAsync(HttpContext httpContext)
    {
        return Task.FromResult(true);
    }

    public Task ValidateRequestAsync(HttpContext httpContext)
    {
        return Task.CompletedTask;
    }

    public void SetCookieTokenAndHeader(HttpContext httpContext)
    {
    }
}
