using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Uri: {Href}")]
public class AnchorTag
{
    public required string Href { get; set; }
    public required Uri Website { get; set; }
    public string? InnerText { get; set; }
    public string? Title { get; set; }
    public required Uri Url { get; set; }

    public override string ToString() => Href;
}
