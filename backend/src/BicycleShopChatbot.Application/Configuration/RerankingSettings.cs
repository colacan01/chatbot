namespace BicycleShopChatbot.Application.Configuration;

public class RerankingSettings
{
    // ONNX settings
    public string ModelPath { get; set; } = "./models/bge-reranker-v2-m3";
    public int MaxCandidates { get; set; } = 20;
    public int TopK { get; set; } = 5;
    public bool EnableGpu { get; set; } = false;
    public int BatchSize { get; set; } = 10;

    // Provider selection: "Onnx" or "Ollama"
    public RerankingProvider Provider { get; set; } = RerankingProvider.Ollama;

    // Ollama-specific settings
    public OllamaRerankingOptions Ollama { get; set; } = new();
}

public enum RerankingProvider
{
    Onnx,
    Ollama
}

public class OllamaRerankingOptions
{
    public string BaseUrl { get; set; } = "http://172.30.1.40:11434";
    public string ModelName { get; set; } = "exaone3.5:7.8b";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxRetries { get; set; } = 2;
    public double Temperature { get; set; } = 0.1;
    public int BatchSize { get; set; } = 5;
}
