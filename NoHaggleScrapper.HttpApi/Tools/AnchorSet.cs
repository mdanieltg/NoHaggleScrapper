using System.Collections;
using NoHaggleScrapper.HttpApi.Models;

namespace NoHaggleScrapper.HttpApi.Tools;

public class AnchorSet : IEnumerable<AnchorHolder>
{
    private readonly Dictionary<string, AnchorHolder> _anchors = new();

    public void Add(AnchorHolder holder)
    {
        if (_anchors.ContainsKey(holder.AnchorTag.Href)) return;
        _anchors.Add(holder.AnchorTag.Href, holder);
    }

    public void AddRange(IEnumerable<AnchorHolder> anchorHolders)
    {
        foreach (AnchorHolder anchorHolder in anchorHolders)
            if (!_anchors.ContainsKey(anchorHolder.AnchorTag.Href))
                _anchors.Add(anchorHolder.AnchorTag.Href, anchorHolder);
    }

    public void AddRange(IEnumerable<AnchorTag> anchorTags)
    {
        foreach (AnchorTag anchorTag in anchorTags)
            if (!_anchors.ContainsKey(anchorTag.Href))
                _anchors.Add(anchorTag.Href, new AnchorHolder
                {
                    AnchorTag = anchorTag
                });
    }

    public IEnumerator<AnchorHolder> GetEnumerator()
    {
        return _anchors.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
