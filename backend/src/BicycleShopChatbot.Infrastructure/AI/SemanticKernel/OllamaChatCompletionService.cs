using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace BicycleShopChatbot.Infrastructure.AI.SemanticKernel;

public class OllamaChatCompletionService : IChatCompletionService
{
    private readonly string _baseUrl;
    private readonly string _modelName;
    private readonly HttpClient _httpClient;

    public OllamaChatCompletionService(string baseUrl, string modelName, HttpClient httpClient)
    {
        _baseUrl = baseUrl;
        _modelName = modelName;
        _httpClient = httpClient;
    }

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
    {
        ["ModelId"] = _modelName,
        ["Provider"] = "Ollama"
    };

    public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _modelName,
            messages = chatHistory.Select(m => new
            {
                role = m.Role.Label.ToLowerInvariant(),
                content = m.Content
            }),
            stream = false,
            options = new
            {
                temperature = executionSettings?.ExtensionData?.TryGetValue("temperature", out var temp) == true
                    ? Convert.ToDouble(temp) : 0.7,
                num_predict = executionSettings?.ExtensionData?.TryGetValue("max_tokens", out var max) == true
                    ? Convert.ToInt32(max) : 2000
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/chat",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);

        return new[]
        {
            new ChatMessageContent(AuthorRole.Assistant, result!.Message.Content)
            {
                ModelId = _modelName
            }
        };
    }

    public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
        ChatHistory chatHistory,
        PromptExecutionSettings? executionSettings = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _modelName,
            messages = chatHistory.Select(m => new { role = m.Role.Label.ToLowerInvariant(), content = m.Content }),
            stream = true,
            options = new
            {
                temperature = executionSettings?.ExtensionData?.TryGetValue("temperature", out var temp) == true
                    ? Convert.ToDouble(temp) : 0.7,
                num_predict = executionSettings?.ExtensionData?.TryGetValue("max_tokens", out var max) == true
                    ? Convert.ToInt32(max) : 2000
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/chat")
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (chunk?.Message?.Content != null)
            {
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, chunk.Message.Content)
                {
                    ModelId = _modelName
                };
            }

            if (chunk?.Done == true) break;
        }
    }

    private class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage Message { get; set; } = new();

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
