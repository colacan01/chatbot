using Pgvector;

namespace BicycleShopChatbot.Domain.Entities;

public class ProductEmbedding
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameKorean { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? DescriptionKorean { get; set; }
    public string? DetailedDescription { get; set; }
    public string Specifications { get; set; } = "{}";
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public string? ImageUrl { get; set; }

    // Vector embedding (768 dimensions)
    public Vector? Embedding { get; set; }

    // Searchable text used for embedding generation
    public string? SearchableText { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
