using System.Diagnostics;

namespace NoHaggleScrapper.HttpApi.Tools;

public class IntervalStopwatch
{
    private readonly Stopwatch _stopwatch = new();
    private readonly List<long> _intervals = new();

    public long Elapsed =>
        _intervals.Count switch
        {
            0 => 0,
            1 => LatestInterval,
            _ => _intervals[_intervals.Count - 1] - _intervals[_intervals.Count - 2]
        };

    public long TotalElapsed => _stopwatch.ElapsedMilliseconds;
    public long LatestInterval { get; set; }

    public void Start() => _stopwatch.Start();

    public void Stop()
    {
        _stopwatch.Stop();
        Interval();
    }

    public void Reset()
    {
        _stopwatch.Reset();
        _intervals.Clear();
        LatestInterval = default;
    }

    public long Interval()
    {
        LatestInterval = _stopwatch.ElapsedMilliseconds;
        _intervals.Add(LatestInterval);
        return LatestInterval;
    }

    public static IntervalStopwatch StartNew()
    {
        IntervalStopwatch stopwatch = new();
        stopwatch.Start();
        return stopwatch;
    }
}
