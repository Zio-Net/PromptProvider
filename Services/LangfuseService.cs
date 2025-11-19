using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using PromptProvider.Models;
using PromptProvider.Options;
using Microsoft.Extensions.Options;
using PromptProvider.Interfaces;
using Microsoft.Extensions.Logging;

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
        // Basic authentication with public key as username and secret key as password
        var authValue = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.PublicKey}:{_options.SecretKey}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
    }

    private void ThrowIfNotConfigured()
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("Langfuse is not configured. Please check your appsettings configuration.");
        }
    }

    public async Task<LangfusePromptModel?> GetPromptAsync(
        string promptName,
        int? version = null,
        string? label = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();

        if (string.IsNullOrWhiteSpace(promptName))
        {
            throw new ArgumentException("Prompt name is required.", nameof(promptName));
        }

        // Default to "production" label if neither version nor label is specified
        if (version is null && string.IsNullOrWhiteSpace(label))
        {
            label = "production";
        }

        try
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
            var requestUri = $"/api/public/v2/prompts/{Uri.EscapeDataString(promptName)}{queryString}";

            _logger.LogInformation("Fetching prompt '{PromptName}' from Langfuse (version: {Version}, label: {Label})",
                promptName, version, label);

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Prompt '{PromptName}' not found in Langfuse", promptName);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var prompt = JsonSerializer.Deserialize<LangfusePromptModel>(content, JsonOptions);

            _logger.LogInformation("Successfully fetched prompt '{PromptName}' version {Version}",
                promptName, prompt?.Version);

            return prompt;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching prompt '{PromptName}' from Langfuse", promptName);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize prompt '{PromptName}' from Langfuse", promptName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching prompt '{PromptName}' from Langfuse", promptName);
            throw;
        }
    }

    public async Task<List<LangfusePromptListItem>> GetAllPromptsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();

        try
        {
            _logger.LogInformation("Fetching all prompts from Langfuse");

            var response = await _httpClient.GetAsync("/api/public/v2/prompts", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Deserialize the paginated response
            var paginatedResponse = JsonSerializer.Deserialize<LangfusePromptsListResponse>(content, JsonOptions);
            if (paginatedResponse?.Data != null)
            {
                _logger.LogInformation("Successfully fetched {Count} prompts from Langfuse",
                    paginatedResponse.Data.Count);
                return paginatedResponse.Data;
            }

            _logger.LogWarning("Received empty or null data from Langfuse prompts API");
            return new List<LangfusePromptListItem>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching all prompts from Langfuse");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize prompts from Langfuse");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching all prompts from Langfuse");
            throw;
        }
    }

    public async Task<CreateLangfusePromptResponse> CreatePromptAsync(
        CreateLangfusePromptRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Prompt name is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt content is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            throw new ArgumentException("Prompt type is required.", nameof(request));
        }

        try
        {
            _logger.LogInformation("Creating new prompt version for '{PromptName}' in Langfuse", request.Name);

            var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/public/v2/prompts", httpContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var createdPrompt = JsonSerializer.Deserialize<CreateLangfusePromptResponse>(responseContent, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize created prompt response");

            _logger.LogInformation("Successfully created prompt '{PromptName}' version {Version}",
                createdPrompt.Name, createdPrompt.Version);

            return createdPrompt;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating prompt '{PromptName}' in Langfuse", request.Name);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize prompt '{PromptName}'", request.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating prompt '{PromptName}' in Langfuse", request.Name);
            throw;
        }
    }

    public async Task<LangfusePromptModel> UpdatePromptLabelsAsync(
        string promptName,
        int version,
        UpdatePromptLabelsRequest request,
        CancellationToken cancellationToken = default)
    {
        ThrowIfNotConfigured();

        if (string.IsNullOrWhiteSpace(promptName))
        {
            throw new ArgumentException("Prompt name is required.", nameof(promptName));
        }

        if (version <= 0)
        {
            throw new ArgumentException("Version must be greater than 0.", nameof(version));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.NewLabels is null || request.NewLabels.Length == 0)
        {
            throw new ArgumentException("At least one label is required.", nameof(request));
        }

        try
        {
            _logger.LogInformation("Updating labels for prompt '{PromptName}' version {Version} in Langfuse",
                promptName, version);

            var requestUri = $"/api/public/v2/prompts/{Uri.EscapeDataString(promptName)}/versions/{version}";

            var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Patch, requestUri)
            {
                Content = httpContent
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Prompt '{PromptName}' version {Version} not found in Langfuse",
                    promptName, version);
                throw new InvalidOperationException($"Prompt '{promptName}' version {version} not found");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var updatedPrompt = JsonSerializer.Deserialize<LangfusePromptModel>(responseContent, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize updated prompt response");

            _logger.LogInformation("Successfully updated labels for prompt '{PromptName}' version {Version}",
                updatedPrompt.Name, updatedPrompt.Version);

            return updatedPrompt;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error updating prompt '{PromptName}' version {Version} in Langfuse",
                promptName, version);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize/deserialize prompt '{PromptName}' version {Version}",
                promptName, version);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating prompt '{PromptName}' version {Version} in Langfuse",
                promptName, version);
            throw;
        }
    }
}
