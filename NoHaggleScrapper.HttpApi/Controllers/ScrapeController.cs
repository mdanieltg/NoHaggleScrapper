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
    private readonly Scrapper _scrapper;

    public ScrapeController(ILogger<ScrapeController> logger, Scrapper scrapper)
    {
        _logger = logger;
        _scrapper = scrapper;
    }

    [HttpGet("scrape")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<DigResult>> Scrape()
    {
        IEnumerable<Uri> urls = new List<Uri>()
        {
            new("https://www.google.com", UriKind.Absolute),
            new("https://www.github.com", UriKind.Absolute),
            new("https://www.linkedin.com", UriKind.Absolute),
            new("https://www.microsoft.com", UriKind.Absolute),
        };

        IReadOnlySet<string> keywords = new HashSet<string>()
        {
            "no haggle",
            "no-haggle",
        };

        IReadOnlySet<string> extensionsToIgnore = new HashSet<string>()
        {
            "jpg",
            "jpeg",
            "png",
            "webp",
            "gif",
            "txt",
            "tiff",
        };

        IReadOnlySet<string> wordsToIgnore = new HashSet<string>()
        {
            "facebook.com",
            "twitter.com",
            "youtube.com",
            "google.com",
        };

        await _scrapper.ScrapeAsync(urls, keywords, extensionsToIgnore, wordsToIgnore);
        return Ok();
    }
}
