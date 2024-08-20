using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public class Crawler(ILogger<Crawler> logger, ILogger<WebClient> webClientLogger)
{
    private IReadOnlyDictionary<string, WebClient>? _webClients;

    public async Task<WebResult[]> CrawlAsync(IEnumerable<Uri> urls)
    {
        // Create the Tasks for running the scrapper for each "main" URL
        List<Task<WebResult>> webpageTasks = new();

        if (_webClients is null)
        {
            // Create WebClients for each "main" URL
            Dictionary<string, WebClient> webClients = new();
            foreach (Uri url in urls)
                webClients.Add(url.Host, WebClient.CreateHttpClient(url, webClientLogger));

            _webClients = webClients;

            foreach (WebClient webClient in _webClients.Values)
                webpageTasks.Add(webClient.GetHtml(null));
        }
        else
        {
            foreach (Uri url in urls)
            {
                string urlHost = url.Host;
                if (_webClients.TryGetValue(urlHost, out WebClient? webClient))
                    webpageTasks.Add(webClient.GetHtml(url));
                else
                {
                    logger.LogWarning("Couldn't find a strict WebClient for the URL {Url}, with host {Host}", url,
                        urlHost);

                    WebClient? alternateClient = _webClients
                        .Where(pair => pair.Key.Contains(urlHost) || urlHost.Contains(pair.Key))
                        .Select(pair => pair.Value)
                        .FirstOrDefault();

                    if (alternateClient is not null)
                        webpageTasks.Add(alternateClient.GetHtml(url));
                    else
                        logger.LogError("Couldn't find a WebClient for the URL {Url}, with host {Host}", url, urlHost);
                }
            }
        }

        return await Task.WhenAll(webpageTasks);
    }
}
