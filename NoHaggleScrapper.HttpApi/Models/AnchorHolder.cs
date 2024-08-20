namespace NoHaggleScrapper.HttpApi.Models;

public class AnchorHolder
{
    public required AnchorTag AnchorTag { get; set; }
    public string? PageTitle { get; set; }
    public bool Visited { get; set; }
}
