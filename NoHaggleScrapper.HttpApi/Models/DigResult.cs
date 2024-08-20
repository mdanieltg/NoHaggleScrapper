namespace NoHaggleScrapper.HttpApi.Models;

public class DigResult
{
    public required double Seconds { get; set; }
    public required long Milliseconds { get; set; }
    public required List<ScrapeResult> Results { get; set; }
}
