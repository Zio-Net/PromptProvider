namespace PromptProvider.Options;

public class LangfuseOptions
{
    private string? _baseUrl;
    private string? _publicKey;
    private string? _secretKey;

    public string? BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value?.Trim();
    }

    public string? PublicKey
    {
        get => _publicKey;
        set => _publicKey = value?.Trim();
    }

    public string? SecretKey
    {
        get => _secretKey;
        set => _secretKey = value?.Trim();
    }

    public LangfuseHttpClientOptions HttpClient { get; set; } = new();

    public LangfuseResilienceOptions Resilience { get; set; } = new();

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(BaseUrl) &&
               !string.IsNullOrWhiteSpace(PublicKey) &&
               !string.IsNullOrWhiteSpace(SecretKey);
    }
}

public sealed class LangfuseHttpClientOptions
{
    public int? MaxConnectionsPerServer { get; set; }

    public int? PooledConnectionLifetimeMinutes { get; set; }

    public int? RequestTimeoutSeconds { get; set; }
}

public sealed class LangfuseResilienceOptions
{
    /// <summary>
    /// Enables optional retry behavior for transient errors.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Number of retries after the initial attempt.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff.
    /// </summary>
    public int BaseDelayMs { get; set; } = 200;
}
