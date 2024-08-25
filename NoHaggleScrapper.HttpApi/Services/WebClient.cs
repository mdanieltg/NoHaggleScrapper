using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Services;

public class WebClient : IDisposable
{
    private static readonly HttpClientHandler HttpClientHandler = new()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
    };

    private static readonly MediaTypeWithQualityHeaderValue AcceptEverything = new("*/*");
    private static readonly StringWithQualityHeaderValue AcceptEncodingGzip = new("gzip");
    private static readonly StringWithQualityHeaderValue AcceptEncodingDeflate = new("deflate");
    private static readonly StringWithQualityHeaderValue AcceptLangEnUs = new("en-US", 0.9);
    private static readonly StringWithQualityHeaderValue AcceptLangEn = new("en", 0.8);

    private readonly HttpClient _httpClient;
    private readonly ILogger<WebClient> _logger;
    private short _forbiddenCount;
    private bool _isBlocked;
    private bool _isHttpClientDisposed;

    private WebClient(ILogger<WebClient> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        BaseUrl = httpClient.BaseAddress!;
    }

    public Uri BaseUrl { get; }

    public async Task<WebResult> GetHtml(Uri? uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Use the base address instead of the uri parameter for logging purposes
        //   the BaseAddress property won't be null here because we're forcing it
        //   in the constructor
        Uri callingUri = uri ?? BaseUrl;
        WebResult result = new() { Uri = callingUri, BaseUrl = BaseUrl };

        // If the HttpClient has already been disposed or we are being blocked by the Host, do nothing else
        if (_isHttpClientDisposed || _isBlocked) return result;

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(uri, cancellationToken);
            _logger.LogDebug("Processing URL {Url}...", callingUri);

            response.EnsureSuccessStatusCode();

            result.Html = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Successful response from URL {Url}", callingUri);
        }
        catch (HttpRequestException e) when (e.StatusCode is not null)
        {
            _logger.LogError("An HTTP {StatusCode} error occurred while processing the URL {Url}: {ErrorDescription}",
                             e.StatusCode, callingUri, e.Message);

            if (e.StatusCode == HttpStatusCode.Forbidden && ++_forbiddenCount >= 10)
            {
                _isBlocked = true;
                _logger.LogWarning("The host {Host} is now blocking requests", BaseUrl.Host);
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogError("An error occurred while processing the URL {Url} with description: {ErrorDescription}",
                             callingUri, e.Message);
        }
        catch (ObjectDisposedException)
        {
            // Do nothing
        }

        return result;
    }

    public static WebClient CreateHttpClient(Uri baseAddress, ILogger<WebClient> logger)
    {
        HttpClient httpClient = new(HttpClientHandler)
        {
            BaseAddress = baseAddress,
            DefaultRequestHeaders =
            {
                Accept = { AcceptEverything },
                AcceptEncoding = { AcceptEncodingGzip, AcceptEncodingDeflate },
                AcceptLanguage = { AcceptLangEnUs, AcceptLangEn },
                Connection = { "keep-alive" },
                UserAgent =
                {
                    // Firefox user agent
                    ProductInfoHeaderValue.Parse("Mozilla/5.0"),
                    ProductInfoHeaderValue.Parse("(Windows NT 10.0; Win64; x64; rv:129.0)"),
                    ProductInfoHeaderValue.Parse("Gecko/20100101"),
                    ProductInfoHeaderValue.Parse("Firefox/129.0")
                }
            }
        };

        return new WebClient(logger, httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _isHttpClientDisposed = true;
    }
}
