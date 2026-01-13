namespace BicycleShopChatbot.Application.Configuration;

public class SemanticKernelSettings
{
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string ModelName { get; set; } = "exaone3.5:7.8b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int MaxTokens { get; set; } = 2000;
    public double DefaultTemperature { get; set; } = 0.7;
    public bool Enabled { get; set; } = true;
}
