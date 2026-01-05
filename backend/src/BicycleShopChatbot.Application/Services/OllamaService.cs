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
    private readonly string _modelName;
    private readonly double _temperature;

    public OllamaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
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
                    temperature = _temperature,
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
                temperature = _temperature,
                top_p = 0.9,
                top_k = 40
            }
        };

        _logger.LogInformation("Sending streaming request to Ollama API for model: {ModelName}", _modelName);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/chat");
        httpRequest.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        _logger.LogInformation("🔍 Starting to read stream from Ollama...");
        int lineCount = 0;
        int chunkCount = 0;

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            lineCount++;

            _logger.LogInformation("📝 Line {LineCount}: {Line}", lineCount, line?.Substring(0, Math.Min(100, line?.Length ?? 0)));

            if (string.IsNullOrWhiteSpace(line))
            {
                _logger.LogInformation("⏭️ Skipping empty line {LineCount}", lineCount);
                continue;
            }

            var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            _logger.LogInformation("🔷 Chunk {ChunkCount}: Done={Done}, Content={Content}", 
                chunkCount, 
                chunk?.Done, 
                chunk?.Message?.Content?.Substring(0, Math.Min(50, chunk?.Message?.Content?.Length ?? 0)));

            if (chunk?.Message?.Content != null)
            {
                chunkCount++;
                _logger.LogInformation("✅ Yielding chunk {ChunkCount} with {Length} chars", chunkCount, chunk.Message.Content.Length);
                yield return chunk.Message.Content;
            }

            if (chunk?.Done == true)
            {
                _logger.LogInformation("✅ Streaming completed successfully. Total chunks: {ChunkCount}", chunkCount);
                break;
            }
        }

        _logger.LogInformation("🏁 Stream reading finished. Lines: {LineCount}, Chunks: {ChunkCount}", lineCount, chunkCount);
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
}
