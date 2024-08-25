namespace NoHaggleScrapper.HttpApi.Services;

public class CancellationTokenProvider
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<CancellationTokenProvider> _logger;

    public CancellationTokenProvider(ILogger<CancellationTokenProvider> logger)
    {
        _logger = logger;
    }

    public CancellationToken? Token => _cancellationTokenSource?.Token;

    public async Task<bool> CancelAsync()
    {
        _logger.LogDebug("Requesting cancellation");
        if (_cancellationTokenSource is null)
        {
            _logger.LogDebug("Nothing to cancel");
            return false;
        }

        await _cancellationTokenSource.CancelAsync();
        _logger.LogDebug("Successful cancellation");

        return true;
    }

    public CancellationToken? CreateToken()
    {
        _logger.LogDebug("New CancellationTokenSource creation requested");

        if (_cancellationTokenSource is not null)
        {
            _logger.LogError("There is an operation in progress, please wait a few seconds or cancel it first");
            return null;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _logger.LogDebug("New CancellationTokenSource created");
        return _cancellationTokenSource.Token;
    }

    public void DisposeToken()
    {
        if (_cancellationTokenSource is not null)
        {
            _logger.LogDebug("Disposing CancellationTokenSource");
            _cancellationTokenSource.Dispose();
        }
    }
}
