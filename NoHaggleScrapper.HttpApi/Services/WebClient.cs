using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public class WebClient
{
    private readonly ILogger<WebClient> _logger;
    private readonly HttpClient _httpClient;

    private WebClient(ILogger<WebClient> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        BaseUrl = httpClient.BaseAddress!;
    }

    public Uri BaseUrl { get; }

    public async Task<WebResult> GetHtml(Uri? uri)
    {
        // Use the base address instead of the uri parameter for logging purposes
        //   the BaseAddress property won't be null here because we're forcing it
        //   in the constructor
        Uri callingUri = uri ?? BaseUrl;
        WebResult result = new() { Uri = callingUri, BaseUrl = BaseUrl };
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            _logger.LogDebug("Processing URL {Url}...", callingUri);

            response.EnsureSuccessStatusCode();

            result.Html = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Successful response from URL {Url}", callingUri);
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("An HTTP {StatusCode} error occurred while processing the URL {Url} with description: {ErrorDescription}",
                e.StatusCode, callingUri, e.Message);
        }

        return result;
    }

    public static WebClient CreateHttpClient(Uri baseAddress, ILogger<WebClient> logger)
    {
        HttpClient httpClient = new()
        {
            BaseAddress = baseAddress
        };
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");

        return new WebClient(logger, httpClient);
    }
}
