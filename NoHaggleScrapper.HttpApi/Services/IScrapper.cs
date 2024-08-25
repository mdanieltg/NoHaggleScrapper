using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public interface IScrapper
{
    Task<List<ScrapeResult>> ScrapeAsync(IEnumerable<Uri> websites, IReadOnlySet<string> keywords,
        IReadOnlySet<string> extensionsToIgnore, IReadOnlySet<string> wordsToIgnore,
        CancellationToken cancellationToken);
}
