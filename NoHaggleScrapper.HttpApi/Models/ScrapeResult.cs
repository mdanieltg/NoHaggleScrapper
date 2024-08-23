using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Url: {Uri}")]
public class ScrapeResult
{
    public required Uri BaseUrl { get; init; }
    public required Uri Uri { get; init; }
    public required HashSet<string> Keywords { get; set; }
    public required HashSet<string> PhoneNumbers { get; set; }
}
