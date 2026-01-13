using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Extensions.Logging;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Configuration;
using BicycleShopChatbot.Infrastructure.AI.Tokenization;

namespace BicycleShopChatbot.Infrastructure.AI.Reranking;

public class OnnxRerankingService : IRerankingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly BgeTokenizer _tokenizer;
    private readonly RerankingSettings _settings;
    private readonly ILogger<OnnxRerankingService> _logger;
    private bool _disposed;

    public OnnxRerankingService(
        RerankingSettings settings,
        ILogger<OnnxRerankingService> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var sessionOptions = new SessionOptions
        {
            LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING,
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        if (settings.EnableGpu)
        {
            try
            {
                sessionOptions.AppendExecutionProvider_CUDA(0);
                _logger.LogInformation("GPU acceleration enabled for ONNX reranking");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enable GPU, falling back to CPU");
            }
        }

        var modelPath = Path.Combine(settings.ModelPath, "model.onnx");
        var sentencePiecePath = Path.Combine(settings.ModelPath, "sentencepiece.bpe.model");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX model not found at {modelPath}. Please download the model files. See README.md in the models directory.");
        }

        if (!File.Exists(sentencePiecePath))
        {
            throw new FileNotFoundException($"SentencePiece model not found at {sentencePiecePath}. Please download sentencepiece.bpe.model from BAAI/bge-reranker-v2-m3. See README.md in the models directory.");
        }

        _session = new InferenceSession(modelPath, sessionOptions);
        // Pass directory path - BgeTokenizer will find sentencepiece.bpe.model inside
        _tokenizer = new BgeTokenizer(settings.ModelPath, maxLength: 512);

        _logger.LogInformation("ONNX reranking model loaded from {ModelPath}", settings.ModelPath);
        
        // Log model input metadata for diagnostics
        _logger.LogInformation("ONNX Model Input Metadata:");
        foreach (var input in _session.InputMetadata)
        {
            var dimensions = string.Join(", ", input.Value.Dimensions.Select(d => d < 0 ? "dynamic" : d.ToString()));
            _logger.LogInformation("  - Input: {Name}, Type: {Type}, Dimensions: [{Dims}]",
                input.Key, input.Value.ElementType, dimensions);
        }
        
        // Log model output metadata for diagnostics
        _logger.LogInformation("ONNX Model Output Metadata:");
        foreach (var output in _session.OutputMetadata)
        {
            var dimensions = string.Join(", ", output.Value.Dimensions.Select(d => d < 0 ? "dynamic" : d.ToString()));
            _logger.LogInformation("  - Output: {Name}, Type: {Type}, Dimensions: [{Dims}]",
                output.Key, output.Value.ElementType, dimensions);
        }
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
            return candidates.Select((p, idx) => new RerankResult<Product>
            {
                Item = p,
                RelevanceScore = 1.0 - (idx * 0.05),
                OriginalIndex = idx
            }).ToList();
        }

        var documents = candidates.Select(p =>
            $"{p.NameKorean} {p.Category} {p.Brand} {p.DescriptionKorean ?? ""}"
        ).ToList();

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
            "Re-ranked {Count} products. Top result: {Product} (score: {Score:F3})",
            candidates.Count, results[0].Item.NameKorean, results[0].RelevanceScore);

        return results;
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

        var documents = candidates.Select(f =>
            $"{f.QuestionKorean} {f.AnswerKorean} {f.Keywords ?? ""}"
        ).ToList();

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
            "Re-ranked {Count} FAQs. Top result score: {Score:F3}",
            candidates.Count, results[0].RelevanceScore);

        return results;
    }

    private async Task<List<double>> RerankInternalAsync(
        string query,
        List<string> documents,
        CancellationToken cancellationToken)
    {
        var scores = new List<double>();
        var batches = documents.Chunk(_settings.BatchSize);

        foreach (var batch in batches)
        {
            var batchScores = await ProcessBatchAsync(query, batch.ToList(), cancellationToken);
            scores.AddRange(batchScores);
        }

        return scores;
    }

    private Task<List<double>> ProcessBatchAsync(
        string query,
        List<string> documents,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var scores = new List<double>();

            foreach (var document in documents)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var tokens = _tokenizer.TokenizePair(query, document);

                // Create ONNX tensors with explicit dimensions
                var inputIds = new DenseTensor<long>(
                    tokens.InputIds,
                    new[] { 1, tokens.InputIds.Length });

                var attentionMask = new DenseTensor<long>(
                    tokens.AttentionMask,
                    new[] { 1, tokens.AttentionMask.Length });

                // Run inference - only input_ids and attention_mask are required by the model
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask)
                };

                try
                {
                    using var results = _session.Run(inputs);
                    var output = results.First();

                    // Extract logits - output shape is typically [1, 1] or [1]
                    float logit;
                    if (output.Value is Tensor<float> tensor)
                    {
                        logit = tensor.GetValue(0);
                    }
                    else
                    {
                        var arr = output.AsEnumerable<float>().ToArray();
                        logit = arr[0];
                    }

                    // BGE reranker outputs logits, apply sigmoid for relevance score
                    var score = Sigmoid(logit);
                    scores.Add(score);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during ONNX inference for document");
                    scores.Add(0.0); // Fallback score
                }
            }

            return scores;
        }, cancellationToken);
    }

    private static double Sigmoid(float x) => 1.0 / (1.0 + Math.Exp(-x));

    public void Dispose()
    {
        if (_disposed) return;

        _session?.Dispose();
        // Tokenizer doesn't implement IDisposable in our simple implementation
        _disposed = true;
    }
}
