using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Models;

[DebuggerDisplay("Url: {AnchorTag.FullUrl}")]
public class AnchorHolder
{
    public required AnchorTag AnchorTag { get; set; }
    public bool Visited { get; set; }
}
