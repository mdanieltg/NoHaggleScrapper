using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Url: {FullUrl}")]
public class AnchorTag
{
    public AnchorTag(Uri baseAddress, Uri url) : this(baseAddress, url, null)
    {
    }

    public AnchorTag(Uri baseAddress, Uri url, string? innerText = null, string? title = null)
    {
        Url = url;
        Website = baseAddress;
        InnerText = innerText;
        Title = title;

        FullUrl = url is { IsAbsoluteUri: false }
            ? new Uri(baseAddress, url)
            : Url;

        Host = FullUrl.Host;
    }

    public Uri Url { get; }
    public Uri Website { get; }
    public Uri FullUrl { get; }
    public string Host { get; }
    public string? InnerText { get; set; }
    public string? Title { get; set; }
    public string Href => FullUrl.ToString();

    public override string ToString() => Url.ToString();
}
