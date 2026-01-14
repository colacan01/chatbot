using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using BicycleShopChatbot.Application.Configuration;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Infrastructure.AI.Reranking;

public class OllamaRerankingService : IRerankingService
{
    private readonly HttpClient _httpClient;
    private readonly RerankingSettings _settings;
    private readonly ILogger<OllamaRerankingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string SystemPrompt = @"당신은 검색 관련성 평가 전문가입니다. 사용자 질문과 문서 간의 관련성을 0~10 점수로 평가합니다.

## 평가 기준
- 10점: 질문에 완벽하게 답하는 문서
- 7-9점: 질문과 매우 관련이 높은 문서
- 4-6점: 부분적으로 관련된 문서
- 1-3점: 관련성이 낮은 문서
- 0점: 전혀 관련 없는 문서

## 응답 형식
반드시 아래 JSON 형식으로만 응답하세요. 다른 텍스트는 포함하지 마세요.
{""scores"": [점수1, 점수2, ...]}

## 주의사항
1. 점수는 0~10 사이의 정수만 사용
2. 문서 순서대로 점수 배열 생성
3. JSON 외의 설명이나 주석 금지";

    public OllamaRerankingService(
        HttpClient httpClient,
        RerankingSettings settings,
        ILogger<OllamaRerankingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri(_settings.Ollama.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Ollama.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation(
            "OllamaRerankingService initialized with model: {Model} at {BaseUrl}",
            _settings.Ollama.ModelName, _settings.Ollama.BaseUrl);
    }

    public async Task<List<RerankResult<Product>>> RerankProductsAsync(
        string query,
        List<Product> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return new List<RerankResult<Product>>();
        }

        if (candidates.Count <= topK)
        {
            _logger.LogDebug("Candidate count ({Count}) <= topK ({TopK}), skipping reranking",
                candidates.Count, topK);
            return candidates.Select((p, idx) => new RerankResult<Product>
            {
                Item = p,
                RelevanceScore = 1.0 - (idx * 0.05),
                OriginalIndex = idx
            }).ToList();
        }

        var documents = candidates.Select(BuildProductDocument).ToList();

        try
        {
            var scores = await RerankInternalAsync(query, documents, cancellationToken);

            var results = candidates
                .Select((product, idx) => new RerankResult<Product>
                {
                    Item = product,
                    RelevanceScore = scores[idx],
                    OriginalIndex = idx
                })
                .OrderByDescending(r => r.RelevanceScore)
                .Take(topK)
                .ToList();

            _logger.LogInformation(
                "Re-ranked {Count} products using Ollama LLM. Top: {Product} (score: {Score:F2})",
                candidates.Count, results[0].Item.NameKorean, results[0].RelevanceScore);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama reranking failed, returning original order");
            return candidates.Take(topK).Select((p, idx) => new RerankResult<Product>
            {
                Item = p,
                RelevanceScore = 1.0 - (idx * 0.05),
                OriginalIndex = idx
            }).ToList();
        }
    }

    public async Task<List<RerankResult<FAQ>>> RerankFaqsAsync(
        string query,
        List<FAQ> candidates,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return new List<RerankResult<FAQ>>();
        }

        if (candidates.Count <= topK)
        {
            return candidates.Select((f, idx) => new RerankResult<FAQ>
            {
                Item = f,
                RelevanceScore = 1.0 - (idx * 0.05),
                OriginalIndex = idx
            }).ToList();
        }

        var documents = candidates.Select(BuildFaqDocument).ToList();

        try
        {
            var scores = await RerankInternalAsync(query, documents, cancellationToken);

            var results = candidates
                .Select((faq, idx) => new RerankResult<FAQ>
                {
                    Item = faq,
                    RelevanceScore = scores[idx],
                    OriginalIndex = idx
                })
                .OrderByDescending(r => r.RelevanceScore)
                .Take(topK)
                .ToList();

            _logger.LogInformation(
                "Re-ranked {Count} FAQs using Ollama LLM. Top score: {Score:F2}",
                candidates.Count, results[0].RelevanceScore);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama FAQ reranking failed, returning original order");
            return candidates.Take(topK).Select((f, idx) => new RerankResult<FAQ>
            {
                Item = f,
                RelevanceScore = 1.0 - (idx * 0.05),
                OriginalIndex = idx
            }).ToList();
        }
    }

    private async Task<List<double>> RerankInternalAsync(
        string query,
        List<string> documents,
        CancellationToken cancellationToken)
    {
        var batchSize = _settings.Ollama.BatchSize;
        var batches = documents
            .Select((doc, idx) => (Document: doc, Index: idx))
            .Chunk(batchSize)
            .ToList();

        var allScores = new double[documents.Count];

        foreach (var batch in batches)
        {
            var batchScores = await ProcessBatchAsync(query, batch.ToList(), cancellationToken);
            foreach (var (score, originalIdx) in batchScores)
            {
                allScores[originalIdx] = score;
            }
        }

        return allScores.ToList();
    }

    private async Task<List<(double Score, int OriginalIndex)>> ProcessBatchAsync(
        string query,
        List<(string Document, int Index)> batch,
        CancellationToken cancellationToken)
    {
        var prompt = BuildBatchPrompt(query, batch.Select(b => b.Document).ToList());

        var request = new
        {
            model = _settings.Ollama.ModelName,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = prompt }
            },
            stream = false,
            options = new
            {
                temperature = _settings.Ollama.Temperature,
                top_p = 0.9
            }
        };

        for (int attempt = 1; attempt <= _settings.Ollama.MaxRetries + 1; attempt++)
        {
            try
            {
                _logger.LogDebug(
                    "Sending reranking batch (attempt {Attempt}): {DocCount} documents",
                    attempt, batch.Count);

                var response = await _httpClient.PostAsJsonAsync(
                    "/api/chat",
                    request,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
                    _jsonOptions, cancellationToken);

                if (result?.Message?.Content == null)
                {
                    _logger.LogWarning("Ollama returned empty content, using default scores");
                    return batch.Select(b => (0.5, b.Index)).ToList();
                }

                var scores = ParseScoresFromResponse(result.Message.Content, batch.Count);
                return batch.Zip(scores, (b, s) => (s, b.Index)).ToList();
            }
            catch (HttpRequestException ex) when (attempt <= _settings.Ollama.MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex,
                    "HTTP error on attempt {Attempt}, retrying in {Delay}s",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException ex) when (
                attempt <= _settings.Ollama.MaxRetries &&
                !cancellationToken.IsCancellationRequested)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(ex,
                    "Timeout on attempt {Attempt}, retrying in {Delay}s",
                    attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError("All retry attempts failed, using default scores");
        return batch.Select(b => (0.5, b.Index)).ToList();
    }

    private static string BuildBatchPrompt(string query, List<string> documents)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"## 사용자 질문\n{query}\n");
        sb.AppendLine("## 평가할 문서들");

        for (int i = 0; i < documents.Count; i++)
        {
            sb.AppendLine($"\n### 문서 {i + 1}");
            sb.AppendLine(documents[i]);
        }

        sb.AppendLine($"\n위 {documents.Count}개 문서 각각의 관련성 점수를 JSON 형식으로 응답하세요.");

        return sb.ToString();
    }

    private List<double> ParseScoresFromResponse(string content, int expectedCount)
    {
        try
        {
            // Try to extract JSON from response
            var jsonMatch = Regex.Match(content, @"\{[^{}]*""scores""[^{}]*\[[\d,\s\.]+\][^{}]*\}",
                RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                var parsed = JsonSerializer.Deserialize<ScoresResponse>(
                    jsonMatch.Value, _jsonOptions);

                if (parsed?.Scores != null && parsed.Scores.Count >= expectedCount)
                {
                    return parsed.Scores.Take(expectedCount)
                        .Select(s => Math.Clamp(s / 10.0, 0.0, 1.0))
                        .ToList();
                }
            }

            // Fallback: extract any numbers in sequence
            var numberMatches = Regex.Matches(content, @"\b(\d+(?:\.\d+)?)\b");
            var numbers = numberMatches
                .Cast<Match>()
                .Select(m => double.TryParse(m.Value, out var n) ? n : -1)
                .Where(n => n >= 0 && n <= 10)
                .Take(expectedCount)
                .Select(n => n / 10.0)
                .ToList();

            if (numbers.Count >= expectedCount)
            {
                return numbers;
            }

            _logger.LogWarning(
                "Could not parse {ExpectedCount} scores from response: {Content}",
                expectedCount, content.Length > 200 ? content[..200] : content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing scores from LLM response");
        }

        return Enumerable.Repeat(0.5, expectedCount).ToList();
    }

    private static string BuildProductDocument(Product p)
    {
        return $"제품명: {p.NameKorean}\n" +
               $"카테고리: {p.Category}\n" +
               $"브랜드: {p.Brand}\n" +
               $"가격: {p.Price:N0}원\n" +
               $"설명: {p.DescriptionKorean ?? p.Description ?? ""}";
    }

    private static string BuildFaqDocument(FAQ f)
    {
        return $"질문: {f.QuestionKorean}\n" +
               $"답변: {f.AnswerKorean}\n" +
               $"카테고리: {f.Category ?? ""}\n" +
               $"키워드: {f.Keywords ?? ""}";
    }

    private class OllamaResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class ScoresResponse
    {
        public List<double> Scores { get; set; } = new();
    }
}
