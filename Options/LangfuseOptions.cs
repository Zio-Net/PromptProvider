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

    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(BaseUrl) &&
               !string.IsNullOrWhiteSpace(PublicKey) &&
               !string.IsNullOrWhiteSpace(SecretKey);
    }
}
