namespace BicycleShopChatbot.Infrastructure.AI.Tokenization;

/// <summary>
/// Simplified BGE tokenizer implementation for XLM-RoBERTa based models.
///
/// IMPORTANT: This is a simplified tokenization that works for basic testing.
/// For production use with full model.onnx, you should:
/// 1. Use a proper SentencePiece tokenizer library
/// 2. Consider adding SentencePieceProcessor NuGet package
/// 3. Or use Python interop with HuggingFace tokenizers
///
/// Current implementation provides basic functionality for demonstration.
/// </summary>
public class BgeTokenizer
{
    private readonly int _maxLength;

    // Special token IDs for XLM-RoBERTa (bge-reranker-v2-m3)
    private const int BOS_TOKEN_ID = 0;    // <s> (also used as CLS)
    private const int PAD_TOKEN_ID = 1;    // <pad>
    private const int EOS_TOKEN_ID = 2;    // </s> (also used as SEP)
    private const int UNK_TOKEN_ID = 3;    // <unk>

    // Starting ID for regular tokens (after special tokens)
    private const int TOKEN_ID_START = 1000;

    public BgeTokenizer(string modelPath, int maxLength = 512)
    {
        _maxLength = maxLength;

        // Check if sentencepiece.bpe.model exists for reference
        var sentencePieceModelPath = Path.Combine(
            Path.GetDirectoryName(modelPath) ?? modelPath,
            "sentencepiece.bpe.model");

        if (!File.Exists(sentencePieceModelPath))
        {
            // Log warning but don't fail - simplified tokenizer doesn't require the file
            Console.WriteLine($"Warning: SentencePiece model not found at {sentencePieceModelPath}. " +
                            $"Using simplified tokenization. For production, download sentencepiece.bpe.model from BAAI/bge-reranker-v2-m3.");
        }
    }

    /// <summary>
    /// Tokenizes a query-document pair for the reranker model.
    /// Format: [BOS] query [EOS] [EOS] document [EOS]
    /// XLM-RoBERTa uses double separator between segments.
    ///
    /// NOTE: This implementation uses a simplified character-level tokenization.
    /// For accurate results with the full ONNX model, proper SentencePiece tokenization is required.
    /// </summary>
    public TokenizerOutput TokenizePair(string query, string document)
    {
        // Simplified tokenization: Convert each character to a token ID
        // Real SentencePiece would use subword tokenization
        var queryTokens = SimpleTokenize(query);
        var docTokens = SimpleTokenize(document);

        // Build sequence: <s> query </s></s> document </s>
        // XLM-RoBERTa format uses double separator between segments
        var inputIds = new List<int> { BOS_TOKEN_ID };
        inputIds.AddRange(queryTokens);
        inputIds.Add(EOS_TOKEN_ID);
        inputIds.Add(EOS_TOKEN_ID); // Double separator for XLM-RoBERTa
        inputIds.AddRange(docTokens);
        inputIds.Add(EOS_TOKEN_ID);

        // Truncate to max length if needed
        if (inputIds.Count > _maxLength)
        {
            inputIds = inputIds.Take(_maxLength - 1).ToList();
            inputIds.Add(EOS_TOKEN_ID); // Ensure EOS at end
        }

        var length = inputIds.Count;

        // Create attention mask (1 for all real tokens)
        var attentionMask = Enumerable.Repeat(1L, length).ToArray();

        // Create token type IDs (0 for first segment, 1 for second segment after double separator)
        var tokenTypeIds = new long[length];
        bool inSecondSegment = false;
        int eosCount = 0;

        for (int i = 0; i < length; i++)
        {
            if (inputIds[i] == EOS_TOKEN_ID)
            {
                eosCount++;
                // After seeing 2 EOS tokens (double separator), we're in the second segment
                if (eosCount >= 2)
                {
                    inSecondSegment = true;
                }
            }
            tokenTypeIds[i] = inSecondSegment ? 1L : 0L;
        }

        return new TokenizerOutput
        {
            InputIds = inputIds.Select(id => (long)id).ToArray(),
            AttentionMask = attentionMask,
            TokenTypeIds = tokenTypeIds
        };
    }

    /// <summary>
    /// Simplified tokenization: character-level with hashing.
    /// Real SentencePiece would perform subword tokenization.
    /// </summary>
    private List<int> SimpleTokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<int>();
        }

        var tokens = new List<int>();

        // Convert each character to a token ID
        // Use character code + offset to avoid collision with special tokens
        foreach (var ch in text)
        {
            // For Korean characters (Hangul): Unicode 0xAC00-0xD7AF
            // For English and numbers: Use their Unicode values
            var tokenId = TOKEN_ID_START + ((int)ch % 50000);
            tokens.Add(tokenId);
        }

        return tokens;
    }
}

public class TokenizerOutput
{
    public long[] InputIds { get; set; } = Array.Empty<long>();
    public long[] AttentionMask { get; set; } = Array.Empty<long>();
    public long[] TokenTypeIds { get; set; } = Array.Empty<long>();
}
