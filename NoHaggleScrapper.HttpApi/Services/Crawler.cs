using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public class Crawler(ILogger<Crawler> logger, ILogger<WebClient> webClientLogger)
{
    private IReadOnlyDictionary<string, WebClient>? _webClients;

    public async Task<WebResult[]> CrawlAsync(IEnumerable<AnchorTag> anchorTags, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Create the Tasks for running the scrapper for each "main" URL
        List<Task<WebResult>> webpageTasks = new();

        if (_webClients is null)
        {
            // Create WebClients for each "main" URL
            Dictionary<string, WebClient> webClients = new();
            foreach (AnchorTag anchorTag in anchorTags)
                webClients.Add(anchorTag.Host, WebClient.CreateHttpClient(anchorTag.Url, webClientLogger));

            foreach (WebClient webClient in webClients.Values)
                webpageTasks.Add(webClient.GetHtml(null, cancellationToken));

            _webClients = webClients;
        }
        else
        {
            foreach (AnchorTag anchorTag in anchorTags)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string urlHost = anchorTag.Host;

                if (_webClients.TryGetValue(urlHost, out WebClient? webClient))
                    webpageTasks.Add(webClient.GetHtml(anchorTag.Url, cancellationToken));
                else
                {
                    logger.LogWarning("Couldn't find a strict WebClient for the URL {Url}, with host {Host}",
                        anchorTag.Url, urlHost);

                    WebClient? alternateClient = _webClients
                        .Where(pair => pair.Key.Contains(urlHost) || urlHost.Contains(pair.Key))
                        .Select(pair => pair.Value)
                        .FirstOrDefault();

                    if (alternateClient is not null)
                        webpageTasks.Add(alternateClient.GetHtml(anchorTag.Url, cancellationToken));
                    else
                        logger.LogError("Couldn't find a WebClient for the URL {Url}, with host {Host}", anchorTag.Url,
                            urlHost);
                }
            }
        }

        return await Task.WhenAll(webpageTasks)
            .WaitAsync(cancellationToken);
    }
}
