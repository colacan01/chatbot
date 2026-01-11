using BicycleShopChatbot.Domain.Entities;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Generates a 768-dimension embedding vector for the given text using Ollama nomic-embed-text model
    /// </summary>
    Task<float[]?> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts in batch
    /// </summary>
    Task<List<float[]?>> GenerateEmbeddingsAsync(
        List<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds searchable text from product information (matches VectorDataLoader logic)
    /// </summary>
    string BuildSearchableText(Product product);
}
