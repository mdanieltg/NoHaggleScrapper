using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public interface IWebClient : IDisposable
{
    Task<WebResult> GetHtml(Uri? uri, CancellationToken cancellationToken);
}
