using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NoHaggleScrapper.HttpApi.Models;
using NoHaggleScrapper.HttpApi.Tools;

namespace NoHaggleScrapper.HttpApi.Services;

public class Scrapper(ILogger<Scrapper> logger, Crawler crawler)
{
    private static readonly Regex PhoneNumbers = new(@"\(?\d{3}\)?[- \.]\d{3}[- \.]\d{4}", RegexOptions.Compiled);
    private static readonly Regex Protocols = new(@"^[\w-]+:(\/\/)?", RegexOptions.Multiline | RegexOptions.Compiled);
    private readonly List<ScrapeResult> _scrapeResults = new();

    public async Task<List<ScrapeResult>> ScrapeAsync(IEnumerable<Uri> websites, IReadOnlySet<string> keywords,
        IReadOnlySet<string> extensionsToIgnore, IReadOnlySet<string> wordsToIgnore,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<AnchorTag> anchors = websites.Select(uri => new AnchorTag(uri, uri));

            logger.LogDebug("Starting initial crawling operation");
            var stopwatch = IntervalStopwatch.StartNew();

            // Crawl the initial "main" URLs
            WebResult[] webResults = await crawler.CrawlAsync(anchors, cancellationToken);

            stopwatch.Interval();
            logger.LogDebug("Finished starting crawling operation in {Milliseconds:N0} milliseconds", stopwatch.Elapsed);
            logger.LogDebug("Starting scraping operation");

            // Scrape anchor tags from pages
            AnchorSet anchorSet = new();
            foreach (WebResult webResult in webResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (webResult.Html is null) continue;

                HashSet<string> scrapedKeywords = ScrapeKeywords(webResult.Html, keywords);

                if (scrapedKeywords.Count > 0)
                    _scrapeResults.Add(new ScrapeResult
                    {
                        Url = new Uri(webResult.BaseUrl, webResult.Uri).ToString(),
                        Host = webResult.BaseUrl.Host,
                        Keywords = scrapedKeywords,
                        PhoneNumbers = ScrapePhoneNumbers(webResult.Html)
                    });

                anchorSet.AddRange(
                    ScrapeAnchorTags(webResult.Html, extensionsToIgnore, wordsToIgnore, webResult.BaseUrl)
                );
            }

            stopwatch.Interval();
            logger.LogDebug("Finished starting scraping operation in {Milliseconds:N0} milliseconds", stopwatch.Elapsed);
            logger.LogDebug("Starting sub-scraping ");

            // Massive sub-scraping operation
            await SubScrapeAsync(anchorSet, keywords, extensionsToIgnore, wordsToIgnore, cancellationToken);

            stopwatch.Stop();
            logger.LogDebug("Finished sub-scraping operation in {Milliseconds:N0} milliseconds", stopwatch.Elapsed);
            logger.LogDebug("Whole scraping operation took {Milliseconds:N0} milliseconds to complete",
                stopwatch.TotalElapsed);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("The operation was stopped due to a cancellation event");
        }

        return _scrapeResults;
    }

    private async Task SubScrapeAsync(AnchorSet anchorSet, IReadOnlySet<string> keywords,
        IReadOnlySet<string> extensionsToIgnore, IReadOnlySet<string> wordsToIgnore,
        CancellationToken cancellationToken)
    {
        do
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<AnchorHolder> remainingAnchors = anchorSet
                .Where(holder => !holder.Visited)
                .ToList();

            if (remainingAnchors.Count == 0) break;

            foreach (AnchorHolder anchorHolder in remainingAnchors)
                anchorHolder.Visited = true;

            WebResult[] webResults = await crawler.CrawlAsync(remainingAnchors.Select(holder => holder.AnchorTag),
                cancellationToken);

            foreach (WebResult webResult in webResults)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (webResult.Html is null) continue;

                HashSet<string> scrapedKeywords = ScrapeKeywords(webResult.Html, keywords);

                if (scrapedKeywords.Count > 0)
                    _scrapeResults.Add(new ScrapeResult
                    {
                        Url = webResult.FullUrl.ToString(),
                        Host = webResult.BaseUrl.Host,
                        Keywords = scrapedKeywords,
                        PhoneNumbers = ScrapePhoneNumbers(webResult.Html)
                    });

                anchorSet.AddRange(
                    ScrapeAnchorTags(webResult.Html, extensionsToIgnore, wordsToIgnore, webResult.BaseUrl)
                );
            }
        } while (true);
    }

    private static HashSet<string> ScrapeKeywords(string html, IReadOnlySet<string> keywords)
    {
        HashSet<string> foundKeywords = new();
        foreach (string keyword in keywords)
            if (html.Contains(keyword))
                foundKeywords.Add(keyword);

        return foundKeywords;
    }

    private static HashSet<string> ScrapePhoneNumbers(string html) =>
        PhoneNumbers.Matches(html)
            .Select(match => match.Value)
            .ToHashSet();

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

            // Contains any extension from the list
            if (extensionsToIgnore.Any(ignoredExtension => href.Contains(ignoredExtension))) continue;

            // Contains any ignored word from the list
            if (wordsToIgnore.Any(ignoredWord => href.Contains(ignoredWord))) continue;

            Uri url = CreateUri(href);

            // Points outside of the website
            if (!IsUrlInternal(url, baseAddress)) continue;

            anchorHolders.Add(new AnchorHolder
            {
                AnchorTag = new AnchorTag(baseAddress, url)
                {
                    InnerText = anchor.InnerText,
                    Title = anchor.GetAttributeValue("title", null)
                }
            });
        }

        return anchorHolders;
    }

    private static bool IsUrlInternal(Uri url, Uri baseAddress) => !url.IsAbsoluteUri || baseAddress.IsBaseOf(url);

    private static Uri CreateUri(string link) =>
        link.StartsWith("http")
            ? new Uri(link, UriKind.Absolute)
            : new Uri(link, UriKind.Relative);
}
