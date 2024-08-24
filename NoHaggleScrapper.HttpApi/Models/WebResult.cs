using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Url: {Uri}")]
public class WebResult
{
    public required Uri BaseUrl { get; init; }
    public required Uri Uri { get; init; }
    public string? Html { get; set; }
    public Uri FullUrl => new Uri(BaseUrl, Uri);
}
