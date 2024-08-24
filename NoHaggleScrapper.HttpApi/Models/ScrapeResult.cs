using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Url: {Url}")]
public class ScrapeResult
{
    public required string Host { get; init; }
    public required string Url { get; init; }
    public required HashSet<string> Keywords { get; set; }
    public required HashSet<string> PhoneNumbers { get; set; }
}
