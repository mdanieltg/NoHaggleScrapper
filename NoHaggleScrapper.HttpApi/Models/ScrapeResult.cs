using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Url: {Uri}")]
public class ScrapeResult
{
    public required Uri BaseUrl { get; init; }
    public required Uri Uri { get; init; }
    public string? Html { get; set; }
    public List<string> Keywords { get; set; }
    public HashSet<AnchorTag> AnchorTags { get; set; }
    public List<string> PhoneNumbers { get; set; }
}
