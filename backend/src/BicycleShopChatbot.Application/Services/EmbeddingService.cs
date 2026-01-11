using System.Net.Http.Json;
using System.Text.Json;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Application.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly string _embeddingModel;
    private readonly int _maxRetries;
    private readonly int _retryDelaySeconds;

    public EmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        _maxRetries = int.Parse(configuration["Ollama:MaxRetries"] ?? "3");
        _retryDelaySeconds = int.Parse(configuration["Ollama:RetryDelaySeconds"] ?? "2");

        var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // Embeddings are faster than chat
    }

    public async Task<float[]?> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty text provided for embedding generation");
            return null;
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            var requestBody = new
            {
                model = _embeddingModel,
                input = text
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/embed",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

            if (result.TryGetProperty("embeddings", out var embeddings) &&
                embeddings.GetArrayLength() > 0)
            {
                var embeddingArray = embeddings[0];
                var vectorList = new List<float>();

                foreach (var value in embeddingArray.EnumerateArray())
                {
                    vectorList.Add((float)value.GetDouble());
                }

                var vector = vectorList.ToArray();
                _logger.LogDebug(
                    "Generated embedding with {Dimensions} dimensions for text length {Length}",
                    vector.Length, text.Length);

                return vector;
            }

            _logger.LogWarning("Ollama returned empty embedding response");
            return null;
        },
        cancellationToken);
    }

    public async Task<List<float[]?>> GenerateEmbeddingsAsync(
        List<string> texts,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]?>();

        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);

            // Rate limiting to avoid overwhelming Ollama
            await Task.Delay(100, cancellationToken);
        }

        return embeddings;
    }

    public string BuildSearchableText(Product product)
    {
        // Match VectorDataLoader logic
        var parts = new List<string>
        {
            product.NameKorean,
            product.Name,
            product.Category,
            product.Brand,
            product.DescriptionKorean ?? string.Empty,
            product.Specifications
        };

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private async Task<T?> ExecuteWithRetryAsync<T>(
        Func<Task<T?>> operation,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (attempt < _maxRetries)
            {
                var delay = TimeSpan.FromSeconds(_retryDelaySeconds * Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    ex,
                    "HTTP error on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}s",
                    attempt, _maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding on attempt {Attempt}", attempt);
                if (attempt == _maxRetries) throw;
            }
        }

        return default;
    }
}
