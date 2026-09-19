using BionicSquare.Utility;

namespace BionicSquare.IntegrationTests.Infrastructure;

public static class HttpClientExtensions
{
    public static HttpClient WithUser(this HttpClient client, string userId = "test-user-id", string role = Role.Customer)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RoleHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }

    public static HttpClient WithAdmin(this HttpClient client, string userId = "test-admin-id")
    {
        return client.WithUser(userId, Role.Admin);
    }

    public static HttpClient AsAnonymous(this HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RoleHeader);
        return client;
    }
}
