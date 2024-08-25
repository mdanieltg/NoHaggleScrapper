using Microsoft.AspNetCore.Mvc;
using NoHaggleScrapper.HttpApi.Models;
using NoHaggleScrapper.HttpApi.Services;

namespace NoHaggleScrapper.HttpApi.Controllers;

[ApiController]
[Route("api/scrapper")]
[Produces("application/json")]
public class ScrapeController : ControllerBase
{
    private readonly ILogger<ScrapeController> _logger;
    private readonly IScrapper _scrapper;
    private readonly CancellationTokenProvider _tokenProvider;

    public ScrapeController(ILogger<ScrapeController> logger, IScrapper scrapper,
        CancellationTokenProvider tokenProvider)
    {
        _logger = logger;
        _scrapper = scrapper;
        _tokenProvider = tokenProvider;
    }

    [HttpGet("cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel() =>
        await _tokenProvider.CancelAsync()
            ? NoContent()
            : NotFound();

    [HttpGet("scrape")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ScrapeResult>>> Scrape()
    {
        CancellationToken? token = _tokenProvider.CreateToken();
        if (!token.HasValue) return BadRequest();

        IEnumerable<Uri> urls = new List<Uri>()
        {
            new("https://www.cars.com/", UriKind.Absolute),
            new("https://www.edmunds.com/", UriKind.Absolute),
        };

        IReadOnlySet<string> keywords = new HashSet<string>()
        {
            "no haggle",
            "no-haggle",
        };

        IReadOnlySet<string> extensionsToIgnore = new HashSet<string>()
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif",
            ".txt",
            ".tiff",
        };

        IReadOnlySet<string> wordsToIgnore = new HashSet<string>()
        {
            "facebook.com",
            "twitter.com",
            "youtube.com",
            "google.com",
        };

        IEnumerable<ScrapeResult> scrapeResults = await _scrapper.ScrapeAsync(urls, keywords, extensionsToIgnore,
                                                                              wordsToIgnore, token.Value);

        _tokenProvider.DisposeToken();

        return Ok(scrapeResults);
    }
}
