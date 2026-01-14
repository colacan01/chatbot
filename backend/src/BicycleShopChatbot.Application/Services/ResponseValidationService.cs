using System.Text.RegularExpressions;
using BicycleShopChatbot.Application.Configuration;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BicycleShopChatbot.Application.Services;

public class ResponseValidationService : IResponseValidationService
{
    private readonly IVectorProductRepository _productRepository;
    private readonly ResponseValidationSettings _settings;
    private readonly ILogger<ResponseValidationService> _logger;

    // Regex patterns to extract product codes (e.g., ROAD-001, MTB-002, EBIKE-001)
    private static readonly Regex[] ProductCodePatterns =
    {
        // Pattern: Standard product code format (e.g., "ROAD-001", "MTB-002")
        new(@"\b([A-Z]{2,10}-\d{3})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Pattern: Korean format with "제품코드:" prefix
        new(@"제품코드[:\s]*([A-Z]{2,10}-\d{3})", RegexOptions.Compiled | RegexOptions.IgnoreCase),
    };

    public ResponseValidationService(
        IVectorProductRepository productRepository,
        IOptions<ResponseValidationSettings> settings,
        ILogger<ResponseValidationService> logger)
    {
        _productRepository = productRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ResponseValidationResult> ValidateAndCleanResponseAsync(
        string response,
        CancellationToken cancellationToken = default)
    {
        var result = new ResponseValidationResult
        {
            OriginalResponse = response,
            CleanedResponse = response
        };

        if (!_settings.Enabled || string.IsNullOrWhiteSpace(response))
        {
            return result;
        }

        // 1. Extract all product codes from response
        var extractedCodes = ExtractProductCodes(response).ToList();

        if (!extractedCodes.Any())
        {
            _logger.LogDebug("No product codes found in response");
            return result;
        }

        _logger.LogDebug("Extracted {Count} product codes: {Codes}",
            extractedCodes.Count, string.Join(", ", extractedCodes));

        // 2. Validate against database
        var validationMap = await ValidateProductCodesAsync(extractedCodes, cancellationToken);

        result.ValidCodes = validationMap.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        result.InvalidCodes = validationMap.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();

        // 3. Apply strategy if invalid codes found
        if (result.InvalidCodes.Any())
        {
            if (_settings.LogInvalidCodes)
            {
                _logger.LogWarning(
                    "Found {Count} invalid/hallucinated product codes: {Codes}",
                    result.InvalidCodes.Count,
                    string.Join(", ", result.InvalidCodes));
            }

            result.CleanedResponse = _settings.Strategy switch
            {
                ValidationStrategy.Remove => RemoveInvalidReferences(response, result.InvalidCodes),
                ValidationStrategy.Warn => AddWarningMessage(response, result.InvalidCodes),
                _ => response
            };
        }

        return result;
    }

    public IEnumerable<string> ExtractProductCodes(string response)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in ProductCodePatterns)
        {
            var matches = pattern.Matches(response);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    codes.Add(match.Groups[1].Value.ToUpperInvariant());
                }
            }
        }

        return codes;
    }

    public async Task<Dictionary<string, bool>> ValidateProductCodesAsync(
        IEnumerable<string> productCodes,
        CancellationToken cancellationToken = default)
    {
        var codeList = productCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        if (!codeList.Any())
        {
            return result;
        }

        try
        {
            // Batch query for all product codes
            var existingProducts = await _productRepository.GetByProductCodesAsync(
                codeList, cancellationToken);

            var existingCodes = existingProducts
                .Select(p => p.ProductCode.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var code in codeList)
            {
                result[code] = existingCodes.Contains(code.ToUpperInvariant());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating product codes against database");
            // On error, assume all codes are valid to avoid false positives
            foreach (var code in codeList)
            {
                result[code] = true;
            }
        }

        return result;
    }

    private string RemoveInvalidReferences(string response, IEnumerable<string> invalidCodes)
    {
        var result = response;

        foreach (var code in invalidCodes)
        {
            // Remove sentences containing the invalid product code
            // Pattern matches Korean/English sentences ending with period, exclamation, or question mark
            var sentencePattern = new Regex(
                $@"[^.!?\n]*\b{Regex.Escape(code)}\b[^.!?\n]*[.!?]?\s*",
                RegexOptions.IgnoreCase);

            result = sentencePattern.Replace(result, "");

            // Also remove standalone code mentions with "제품코드:" prefix
            var codeRefPattern = new Regex(
                $@"\s*\(?\s*제품코드[:\s]*{Regex.Escape(code)}\s*\)?\s*",
                RegexOptions.IgnoreCase);

            result = codeRefPattern.Replace(result, " ");
        }

        // Clean up extra whitespace and newlines
        result = Regex.Replace(result, @"\n{3,}", "\n\n");
        result = Regex.Replace(result, @"[ \t]+", " ");
        result = result.Trim();

        return result;
    }

    private string AddWarningMessage(string response, IEnumerable<string> invalidCodes)
    {
        var codeList = string.Join(", ", invalidCodes);
        var warningMessage = $"\n\n⚠️ 주의: 일부 제품 코드({codeList})는 " +
                            "현재 데이터베이스에서 확인할 수 없습니다. " +
                            "정확한 제품 정보는 고객센터(1588-0000)로 문의해 주세요.";

        return response + warningMessage;
    }
}
