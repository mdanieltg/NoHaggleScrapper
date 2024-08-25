using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public class Crawler(ILogger<Crawler> logger, ILogger<WebClient> webClientLogger) : ICrawler
{
    private readonly ILogger<Crawler> _logger = logger;
    private IReadOnlyDictionary<string, IWebClient>? _webClients;

    public async Task<WebResult[]> CrawlAsync(IEnumerable<AnchorTag> anchorTags, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Create the Tasks for running the scrapper for each "main" URL
        List<Task<WebResult>> webpageTasks = new();

        if (_webClients is null)
        {
            // Create WebClients for each "main" URL
            Dictionary<string, IWebClient> webClients = new();
            foreach (AnchorTag anchorTag in anchorTags)
                webClients.Add(anchorTag.Host, WebClient.CreateHttpClient(anchorTag.Url, webClientLogger));

            foreach (IWebClient webClient in webClients.Values)
                webpageTasks.Add(webClient.GetHtml(null, cancellationToken));

            _webClients = webClients;
        }
        else
        {
            foreach (AnchorTag anchorTag in anchorTags)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string urlHost = anchorTag.Host;

                if (_webClients.TryGetValue(urlHost, out IWebClient? webClient))
                    webpageTasks.Add(webClient.GetHtml(anchorTag.Url, cancellationToken));
                else
                {
                    _logger.LogError("Couldn't find a WebClient for the URL {Url}, with host {Host}", anchorTag.Url,
                                     urlHost);
                }
            }
        }

        return await Task.WhenAll(webpageTasks)
            .WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_webClients is not null)
            foreach ((string _, IWebClient value) in _webClients)
                value.Dispose();
    }
}
