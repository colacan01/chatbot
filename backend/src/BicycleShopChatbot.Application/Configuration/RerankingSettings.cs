namespace BicycleShopChatbot.Application.Configuration;

public class RerankingSettings
{
    public string ModelPath { get; set; } = "./models/bge-reranker-v2-m3";
    public int MaxCandidates { get; set; } = 20;
    public int TopK { get; set; } = 5;
    public bool EnableGpu { get; set; } = false;
    public int BatchSize { get; set; } = 10;
}
