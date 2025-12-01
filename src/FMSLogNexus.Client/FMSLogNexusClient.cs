using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FMSLogNexus.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMSLogNexus.Client;

/// <summary>
/// Main client for interacting with FMS Log Nexus API.
/// </summary>
public class FMSLogNexusClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly FMSLogNexusOptions _options;
    private readonly ILogger<FMSLogNexusClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _accessToken;
    private DateTime? _tokenExpiry;

    /// <summary>
    /// Log client for sending log entries.
    /// </summary>
    public LogClient Logs { get; }

    /// <summary>
    /// Server client for heartbeats and server management.
    /// </summary>
    public ServerClient Servers { get; }

    /// <summary>
    /// Job client for job registration and management.
    /// </summary>
    public JobClient Jobs { get; }

    /// <summary>
    /// Execution client for tracking job executions.
    /// </summary>
    public ExecutionClient Executions { get; }

    /// <summary>
    /// Creates a new FMS Log Nexus client.
    /// </summary>
    public FMSLogNexusClient(FMSLogNexusOptions options, ILogger<FMSLogNexusClient>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(_options.HttpTimeoutSeconds)
        };

        ConfigureAuthentication();

        Logs = new LogClient(this);
        Servers = new ServerClient(this);
        Jobs = new JobClient(this);
        Executions = new ExecutionClient(this);
    }

    /// <summary>
    /// Creates a new FMS Log Nexus client with dependency injection.
    /// </summary>
    public FMSLogNexusClient(IOptions<FMSLogNexusOptions> options, ILogger<FMSLogNexusClient>? logger = null)
        : this(options.Value, logger)
    {
    }

    private void ConfigureAuthentication()
    {
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
        }
        else if (!string.IsNullOrEmpty(_options.AccessToken))
        {
            SetAccessToken(_options.AccessToken);
        }
    }

    /// <summary>
    /// Sets the access token for authentication.
    /// </summary>
    public void SetAccessToken(string token, DateTime? expiry = null)
    {
        _accessToken = token;
        _tokenExpiry = expiry;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Authenticates with username and password.
    /// </summary>
    public async Task<AuthResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequest { Username = username, Password = password };
        var response = await PostAsync<AuthResponse>("api/auth/login", request, cancellationToken);
        
        if (response != null)
        {
            SetAccessToken(response.AccessToken, response.ExpiresAt);
        }

        return response!;
    }

    /// <summary>
    /// Checks if the client is authenticated.
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) || !string.IsNullOrEmpty(_options.ApiKey);

    /// <summary>
    /// Checks if the token is expired.
    /// </summary>
    public bool IsTokenExpired => _tokenExpiry.HasValue && _tokenExpiry.Value < DateTime.UtcNow;

    /// <summary>
    /// Gets the server name from options.
    /// </summary>
    public string ServerName => _options.ServerName;

    /// <summary>
    /// Gets the agent version from options.
    /// </summary>
    public string AgentVersion => _options.AgentVersion;

    #region HTTP Methods

    internal async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        });
    }

    internal async Task<T?> PostAsync<T>(string url, object? content, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(url, content, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            
            if (response.Content.Headers.ContentLength == 0)
                return default;
                
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        });
    }

    internal async Task PostAsync(string url, object? content, CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(url, content, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return true;
        });
    }

    internal async Task<T?> PutAsync<T>(string url, object? content, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.PutAsJsonAsync(url, content, _jsonOptions, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            
            if (response.Content.Headers.ContentLength == 0)
                return default;
                
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
        });
    }

    internal async Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
            return true;
        });
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _options.RetryAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Request failed, attempt {Attempt}/{MaxAttempts}", 
                    attempt + 1, _options.RetryAttempts + 1);

                if (attempt < _options.RetryAttempts)
                {
                    await Task.Delay(_options.RetryDelayMs * (attempt + 1));
                }
            }
        }

        throw lastException!;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            ApiError? error = null;

            try
            {
                error = JsonSerializer.Deserialize<ApiError>(content, _jsonOptions);
            }
            catch
            {
                // Ignore deserialization errors
            }

            var message = error?.Detail ?? error?.Title ?? content;
            throw new FMSLogNexusException(message, (int)response.StatusCode);
        }
    }

    #endregion

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Exception thrown by FMS Log Nexus client.
/// </summary>
public class FMSLogNexusException : Exception
{
    public int StatusCode { get; }

    public FMSLogNexusException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}
