using AngleSharp;
using AngleSharp.Html.Dom;

namespace BionicSquare.IntegrationTests.Infrastructure;

public static class HtmlHelpers
{
    public static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req => req.Content(content));
        return (IHtmlDocument)document;
    }
}
