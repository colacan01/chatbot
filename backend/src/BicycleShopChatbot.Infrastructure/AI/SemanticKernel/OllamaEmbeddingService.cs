#pragma warning disable SKEXP0001
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace BicycleShopChatbot.Infrastructure.AI.SemanticKernel;

public class OllamaEmbeddingService : ITextEmbeddingGenerationService
{
    private readonly string _baseUrl;
    private readonly string _modelName;
    private readonly HttpClient _httpClient;

    public OllamaEmbeddingService(string baseUrl, string modelName, HttpClient httpClient)
    {
        _baseUrl = baseUrl;
        _modelName = modelName;
        _httpClient = httpClient;
    }

    public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
    {
        ["ModelId"] = _modelName
    };

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<ReadOnlyMemory<float>>();

        foreach (var text in data)
        {
            var request = new { model = _modelName, input = text };
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/embed",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(cancellationToken);

            if (result?.Embeddings != null && result.Embeddings.Length > 0)
            {
                embeddings.Add(new ReadOnlyMemory<float>(result.Embeddings));
            }
        }

        return embeddings;
    }

    private class OllamaEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public float[] Embeddings { get; set; } = Array.Empty<float>();

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
