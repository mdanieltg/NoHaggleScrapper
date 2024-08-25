using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public interface ICrawler : IDisposable
{
    Task<WebResult[]> CrawlAsync(IEnumerable<AnchorTag> anchorTags, CancellationToken cancellationToken);
}
