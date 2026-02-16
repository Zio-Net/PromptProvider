using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptProvider.Interfaces;
using PromptProvider.Models;
using PromptProvider.Options;

namespace PromptProvider.Services;

public class LangfuseService : ILangfuseService
{
    private readonly ILogger<LangfuseService> _logger;
    private readonly HttpClient _httpClient;
    private readonly LangfuseOptions _options;
    private readonly bool _isConfigured;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LangfuseService(
        ILogger<LangfuseService> logger,
        HttpClient httpClient,
        IOptions<LangfuseOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
        _isConfigured = _options.IsConfigured();

        if (_isConfigured)
        {
            ConfigureHttpClient();
        }
        else
        {
            _logger.LogWarning("Langfuse is not configured. Service will not be available.");
        }
    }

    private void ConfigureHttpClient()
    {
        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.PublicKey}:{_options.SecretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Langfuse BaseUrl is not configured.");
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);

        if (_options.HttpClient.RequestTimeoutSeconds is > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.HttpClient.RequestTimeoutSeconds.Value);
        }
    }

    private void ThrowIfNotConfigured()
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("Langfuse is not configured. Please check your appsettings configuration.");
        }
    }

    public async Task<LangfusePromptModel?> GetPromptAsync(string promptName, int? version = null, string? label = null, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();
        ValidatePromptName(promptName);

        if (version is null && string.IsNullOrWhiteSpace(label))
        {
            label = "production";
        }

        var requestUri = BuildPromptRequestUri(promptName, version, label);
        using var response = await SendWithRetryAsync(_ => _httpClient.GetAsync(requestUri, cancellationToken), promptName, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LangfusePromptModel>(JsonOptions, cancellationToken);
    }

    public async Task<LangfuseChatPromptModel?> GetChatPromptAsync(string promptName, int? version = null, string? label = null, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();
        ValidatePromptName(promptName);

        if (version is null && string.IsNullOrWhiteSpace(label))
        {
            label = "production";
        }

        var requestUri = BuildPromptRequestUri(promptName, version, label);
        using var response = await SendWithRetryAsync(_ => _httpClient.GetAsync(requestUri, cancellationToken), promptName, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LangfuseChatPromptModel>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();

        using var response = await SendWithRetryAsync(_ => _httpClient.GetAsync("/api/public/v2/prompts", cancellationToken), "*", cancellationToken);
        response.EnsureSuccessStatusCode();

        var paginatedResponse = await response.Content.ReadFromJsonAsync<LangfusePromptsListResponse>(JsonOptions, cancellationToken);
        return paginatedResponse?.Data ?? [];
    }

    public async Task<CreateLangfusePromptResponse> CreatePromptAsync(CreateLangfusePromptRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();
        ArgumentNullException.ThrowIfNull(request);
        ValidatePromptName(request.Name);
        if (string.IsNullOrWhiteSpace(request.Prompt)) throw new ArgumentException("Prompt content is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Type)) throw new ArgumentException("Prompt type is required.", nameof(request));

        using var response = await SendWithRetryAsync(
            _ => _httpClient.PostAsJsonAsync("/api/public/v2/prompts", request, JsonOptions, cancellationToken),
            request.Name,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateLangfusePromptResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created prompt response");
    }

    public async Task<CreateLangfuseChatPromptResponse> CreateChatPromptAsync(CreateLangfuseChatPromptRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();
        ArgumentNullException.ThrowIfNull(request);
        ValidatePromptName(request.Name);
        if (request.Prompt is null || request.Prompt.Length == 0) throw new ArgumentException("Chat messages are required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Type)) throw new ArgumentException("Prompt type is required.", nameof(request));

        using var response = await SendWithRetryAsync(
            _ => _httpClient.PostAsJsonAsync("/api/public/v2/prompts", request, JsonOptions, cancellationToken),
            request.Name,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateLangfuseChatPromptResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize created chat prompt response");
    }

    public async Task<LangfusePromptModel> UpdatePromptLabelsAsync(string promptName, int version, UpdatePromptLabelsRequest request, CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();
        ValidatePromptName(promptName);
        if (version <= 0) throw new ArgumentException("Version must be greater than 0.", nameof(version));
        ArgumentNullException.ThrowIfNull(request);
        if (request.NewLabels is null || request.NewLabels.Length == 0) throw new ArgumentException("At least one label is required.", nameof(request));

        var requestUri = $"/api/public/v2/prompts/{Uri.EscapeDataString(promptName)}/versions/{version}";
        using var response = await SendWithRetryAsync(async attempt =>
            {
                using var message = new HttpRequestMessage(HttpMethod.Patch, requestUri)
                {
                    Content = JsonContent.Create(request, options: JsonOptions)
                };
                return await _httpClient.SendAsync(message, cancellationToken);
            }, promptName, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"Prompt '{promptName}' version {version} not found");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LangfusePromptModel>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize updated prompt response");
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<int, Task<HttpResponseMessage>> send, string promptKey, CancellationToken cancellationToken)
    {
        var retriesEnabled = _options.Resilience.Enabled;
        var maxRetries = retriesEnabled ? Math.Max(0, _options.Resilience.MaxRetries) : 0;
        var baseDelayMs = Math.Max(1, _options.Resilience.BaseDelayMs);

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await send(attempt);
                if (attempt >= maxRetries || !ShouldRetry(response.StatusCode))
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(PromptLoggingEvents.RetryAttempt, ex,
                    "Transient HTTP error on attempt {Attempt} for PromptKey '{PromptKey}'. Retrying.", attempt + 1, promptKey);
            }

            var delay = TimeSpan.FromMilliseconds(baseDelayMs * Math.Pow(2, attempt));
            _logger.LogInformation(PromptLoggingEvents.RetryAttempt,
                "Retrying request for PromptKey '{PromptKey}' in {DelayMs}ms (attempt {Attempt}/{MaxRetries})",
                promptKey, delay.TotalMilliseconds, attempt + 1, maxRetries);
            await Task.Delay(delay, cancellationToken);
        }
    }

    private static string BuildPromptRequestUri(string promptName, int? version, string? label)
    {
        var queryParams = new List<string>();
        if (version.HasValue)
        {
            queryParams.Add($"version={version.Value}");
        }

        if (!string.IsNullOrWhiteSpace(label))
        {
            queryParams.Add($"label={Uri.EscapeDataString(label)}");
        }

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        return $"/api/public/v2/prompts/{Uri.EscapeDataString(promptName)}{queryString}";
    }

    private static void ValidatePromptName(string promptName)
    {
        if (string.IsNullOrWhiteSpace(promptName))
        {
            throw new ArgumentException("Prompt name is required.", nameof(promptName));
        }
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.RequestTimeout
           || statusCode == (HttpStatusCode)429
           || (int)statusCode >= 500;
}
