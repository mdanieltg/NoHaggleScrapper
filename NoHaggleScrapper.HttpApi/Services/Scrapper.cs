using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NoHaggleScrapper.HttpApi.Models;
using NoHaggleScrapper.HttpApi.Tools;

namespace NoHaggleScrapper.HttpApi.Services;

public class Scrapper(ILogger<Scrapper> logger, Crawler crawler)
{
    private static readonly Regex PhoneNumbers = new(@"\(?\d{3}\)?[- \.]\d{3}[- \.]\d{4}", RegexOptions.Compiled);
    private static readonly Regex Protocols = new(@"^[\w-]+:\/\/", RegexOptions.Multiline & RegexOptions.Compiled);

    public async Task ScrapeAsync(IEnumerable<Uri> websites, IReadOnlySet<string> keywords,
        IReadOnlySet<string> extensionsToIgnore, IReadOnlySet<string> wordsToIgnore)
    {
        logger.LogDebug("Starting initial crawling operation");
        var stopwatch = IntervalStopwatch.StartNew();

        // Crawl the initial "main" URLs
        WebResult[] webResults = await crawler.CrawlAsync(websites);

        stopwatch.Interval();
        logger.LogDebug("Finished starting crawling operation in {Milliseconds} milliseconds", stopwatch.Elapsed);
        logger.LogDebug("Starting scraping operation");

        // Scrape anchor tags from pages
        AnchorSet anchorSet = new();
        foreach (WebResult webResult in webResults)
        {
            if (webResult.Html is null) continue;
            anchorSet.AddRange(
                ScrapeAnchorTags(webResult.Html, extensionsToIgnore, wordsToIgnore, webResult.BaseUrl)
            );
        }

        stopwatch.Interval();
        logger.LogDebug("Finished starting scraping operation in {Milliseconds} milliseconds", stopwatch.Elapsed);
        logger.LogDebug("Starting sub-scraping ");

        // Massive sub-scraping operation
        List<ScrapeResult> scrapeResult = await SubScrapeAsync(anchorSet, extensionsToIgnore, wordsToIgnore);

        stopwatch.Stop();
        logger.LogDebug("Finished sub-scraping operation in {Milliseconds} milliseconds", stopwatch.Elapsed);
        logger.LogDebug("Whole scraping operation took {Milliseconds:N0} milliseconds to complete",
            stopwatch.TotalElapsed);
    }

    private async Task<List<ScrapeResult>> SubScrapeAsync(AnchorSet anchorSet, IReadOnlySet<string> extensionsToIgnore,
        IReadOnlySet<string> wordsToIgnore)
    {
        List<AnchorHolder> remainingAnchors;
        do
        {
            remainingAnchors = anchorSet
                .Where(holder => !holder.Visited)
                .ToList();

            if (remainingAnchors.Count == 0) break;

            foreach (AnchorHolder anchorHolder in remainingAnchors)
                anchorHolder.Visited = true;

            WebResult[] webResults = await crawler.CrawlAsync(remainingAnchors.Select(holder => holder.AnchorTag.Url));

            foreach (WebResult webResult in webResults)
            {
                if (webResult.Html is null) continue;
                anchorSet.AddRange(
                    ScrapeAnchorTags(webResult.Html, extensionsToIgnore, wordsToIgnore, webResult.BaseUrl)
                );
            }
        } while (true);

        throw new NotImplementedException();
    }

    private static List<string> ScrapeKeywords(string html)
    {
        throw new NotImplementedException();
    }

    private static List<string> ScrapePhoneNumbers(string html) =>
        PhoneNumbers.Matches(html)
            .Cast<Match>()
            .Select(match => match.Value)
            .ToList();

    private static List<AnchorHolder> ScrapeAnchorTags(string html, IReadOnlySet<string> extensionsToIgnore,
        IReadOnlySet<string> wordsToIgnore, Uri baseAddress)
    {
        List<AnchorHolder> anchorHolders = new();
        HtmlDocument document = new();

        document.LoadHtml(html);
        IEnumerable<HtmlNode> anchors = document.DocumentNode.Descendants("a");

        foreach (HtmlNode anchor in anchors)
        {
            // href attribute missing
            string? href = anchor.GetAttributeValue("href", null);
            if (string.IsNullOrEmpty(href)) continue;

            // points to the same page or host
            if (href.StartsWith('#') || href == "/") continue;

            // Protocols different than http and https
            if (Protocols.IsMatch(href) && !href.StartsWith("http")) continue;

            // Points outside of the website
            if (!IsLinkInternal(href, baseAddress.Host)) continue;

            // Contains any extension from the list
            if (extensionsToIgnore.Any(ignoredExtension => href.Contains(ignoredExtension))) continue;

            // Contains any ignored word from the list
            if (wordsToIgnore.Any(ignoredWord => href.Contains(ignoredWord))) continue;

            anchorHolders.Add(new AnchorHolder
            {
                AnchorTag = new AnchorTag
                {
                    Href = href,
                    Url = CreateUri(href),
                    Website = baseAddress,
                    InnerText = anchor.InnerText,
                    Title = anchor.GetAttributeValue("title", null)
                }
            });
        }

        return anchorHolders;
    }

    private static bool IsLinkInternal(string link, string baseAddress)
    {
        // Relative, always internal
        if (link.StartsWith("/"))
            return true;

        // Absolute, validate Host
        else if (link.StartsWith("http"))
            return link.Contains(baseAddress) || baseAddress.Contains(link);

        // Probably relative
        return true;
    }

    private static Uri CreateUri(string link) =>
        link.StartsWith("http")
            ? new Uri(link, UriKind.Absolute)
            : new Uri(link, UriKind.Relative);
}
