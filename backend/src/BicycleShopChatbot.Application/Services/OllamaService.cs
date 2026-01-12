using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Application.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _modelName;
    private readonly double _temperature;

    public OllamaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _modelName = configuration["Ollama:ModelName"] ?? "qwen2.5:14b";
        _temperature = double.Parse(configuration["Ollama:DefaultTemperature"] ?? "0.7");

        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(
            int.Parse(configuration["Ollama:TimeoutSeconds"] ?? "120"));
    }

    public async Task<string> GenerateResponseAsync(
        string userMessage,
        List<ChatMessageDto> conversationHistory,
        string systemPrompt,
        double temperature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = BuildMessagePayload(userMessage, conversationHistory, systemPrompt);

            var request = new
            {
                model = _modelName,
                messages = messages,
                stream = false,
                options = new
                {
                    temperature = temperature,
                    top_p = 0.9,
                    top_k = 40
                }
            };

            _logger.LogInformation("Sending request to Ollama API for model: {ModelName}", _modelName);

            var response = await _httpClient.PostAsJsonAsync(
                "/api/chat",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken);

            if (result?.Message?.Content == null)
            {
                _logger.LogWarning("Ollama returned empty response");
                return "죄송합니다. 응답을 생성하는 중 문제가 발생했습니다.";
            }

            _logger.LogInformation("Successfully received response from Ollama");
            return result.Message.Content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while communicating with Ollama");
            return "죄송합니다. AI 서비스와 통신 중 오류가 발생했습니다.";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request to Ollama timed out");
            return "죄송합니다. 요청 시간이 초과되었습니다. 잠시 후 다시 시도해주세요.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling Ollama");
            return "죄송합니다. 예기치 않은 오류가 발생했습니다.";
        }
    }

    public async IAsyncEnumerable<string> GenerateResponseStreamAsync(
        string userMessage,
        List<ChatMessageDto> conversationHistory,
        string systemPrompt,
        double temperature,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = BuildMessagePayload(userMessage, conversationHistory, systemPrompt);

        var request = new
        {
            model = _modelName,
            messages = messages,
            stream = true,  // 스트리밍 활성화
            options = new
            {
                temperature = temperature,
                top_p = 0.9,
                top_k = 40
            }
        };

        _logger.LogInformation("Sending streaming request to Ollama API for model: {ModelName}", _modelName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
        httpRequest.Content = JsonContent.Create(request);

        // 헤더 추가
        httpRequest.Headers.Add("Accept", "application/x-ndjson");
        httpRequest.Headers.ConnectionClose = false;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            // 상태 코드 확인 강화
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Ollama API returned {StatusCode}: {ErrorContent}",
                    response.StatusCode,
                    errorContent);
                throw new HttpRequestException(
                    $"Ollama API error: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "HTTP connection error. Check network to {BaseUrl}",
                _httpClient.BaseAddress);
            throw new InvalidOperationException(
                $"Failed to connect to Ollama at {_httpClient.BaseAddress}. " +
                "Verify server is running and accessible.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Streaming connection timeout");
            throw new TimeoutException(
                "Streaming connection timed out. Server may be overloaded.", ex);
        }

        using (response)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                _logger.LogInformation("Successfully started stream from Ollama");
                int lineCount = 0;
                int chunkCount = 0;

                while (!reader.EndOfStream)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = await reader.ReadLineAsync();
                    lineCount++;

                    _logger.LogDebug("Line {LineCount}: {Line}",
                        lineCount,
                        line?.Substring(0, Math.Min(100, line?.Length ?? 0)));

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    OllamaStreamResponse? chunk;
                    try
                    {
                        chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse line {LineCount}: {Line}", lineCount, line);
                        continue;
                    }

                    if (chunk?.Message?.Content != null)
                    {
                        chunkCount++;
                        _logger.LogDebug("Yielding chunk {ChunkCount} with {Length} chars",
                            chunkCount,
                            chunk.Message.Content.Length);
                        yield return chunk.Message.Content;
                    }

                    if (chunk?.Done == true)
                    {
                        _logger.LogInformation(
                            "Streaming completed successfully. Total chunks: {ChunkCount}",
                            chunkCount);
                        break;
                    }
                }

                _logger.LogInformation("Stream reading finished. Lines: {LineCount}, Chunks: {ChunkCount}",
                    lineCount, chunkCount);
            }
        }
    }

    public async Task<bool> IsModelAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken);

            return result?.Models?.Any(m => m.Name?.Contains(_modelName.Split(':')[0]) == true) ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Ollama model is available");
            return false;
        }
    }

    public async Task<OllamaHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var status = new OllamaHealthStatus
        {
            BaseUrl = _httpClient.BaseAddress?.ToString() ?? "Not configured",
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            sw.Stop();

            status.IsReachable = response.IsSuccessStatusCode;
            status.ResponseTimeMs = (int)sw.ElapsedMilliseconds;

            if (status.IsReachable)
            {
                var result = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(cancellationToken);
                status.IsModelAvailable = result?.Models?.Any(m =>
                    m.Name?.Contains(_modelName.Split(':')[0]) == true) ?? false;
                status.AvailableModels = result?.Models?.Select(m => m.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
            }

            _logger.LogInformation(
                "Ollama health check: Reachable={IsReachable}, ModelAvailable={IsModelAvailable}, ResponseTime={ResponseTimeMs}ms",
                status.IsReachable, status.IsModelAvailable, status.ResponseTimeMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Ollama at {BaseUrl}", _httpClient.BaseAddress);
            status.IsReachable = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    private List<object> BuildMessagePayload(
        string userMessage,
        List<ChatMessageDto> history,
        string systemPrompt)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        foreach (var msg in history.TakeLast(10))
        {
            messages.Add(new
            {
                role = msg.Role.ToLowerInvariant(),
                content = msg.Content
            });
        }

        messages.Add(new { role = "user", content = userMessage });

        return messages;
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        string operationName)
    {
        var maxRetries = int.Parse(_configuration["Ollama:MaxRetries"] ?? "3");
        var retryDelay = int.Parse(_configuration["Ollama:RetryDelaySeconds"] ?? "2");

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Executing {Operation}, attempt {Attempt}/{MaxRetries}",
                    operationName, attempt, maxRetries);

                return await operation(cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(retryDelay * Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    ex,
                    "HTTP error on attempt {Attempt}/{MaxRetries} for {Operation}. Retrying in {Delay}s",
                    attempt, maxRetries, operationName, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(retryDelay * Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    ex,
                    "Timeout on attempt {Attempt}/{MaxRetries} for {Operation}. Retrying in {Delay}s",
                    attempt, maxRetries, operationName, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogInformation("Final attempt {MaxRetries} for {Operation}", maxRetries, operationName);
        return await operation(cancellationToken);
    }

    private class OllamaResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaStreamResponse
    {
        public OllamaMessage? Message { get; set; }
        public bool Done { get; set; }
    }

    private class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class OllamaTagsResponse
    {
        public List<OllamaModel>? Models { get; set; }
    }

    private class OllamaModel
    {
        public string? Name { get; set; }
    }

    public class OllamaHealthStatus
    {
        public string BaseUrl { get; set; } = string.Empty;
        public bool IsReachable { get; set; }
        public bool IsModelAvailable { get; set; }
        public int ResponseTimeMs { get; set; }
        public DateTime CheckedAt { get; set; }
        public List<string>? AvailableModels { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
