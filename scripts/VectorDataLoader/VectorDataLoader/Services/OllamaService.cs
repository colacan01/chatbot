using System.Text;
using System.Text.Json;
using VectorDataLoader.Models;

namespace VectorDataLoader.Services;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private const string LlmModel = "exaone3.5:7.8b";
    private const string EmbeddingModel = "nomic-embed-text";

    public OllamaService(string baseUrl = "http://localhost:11434")
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10) // 긴 타임아웃 설정
        };
    }

    /// <summary>
    /// 제품 정보를 기반으로 상세 설명을 생성합니다.
    /// </summary>
    public async Task<string> GenerateProductDescriptionAsync(Product product)
    {
        var prompt = $@"다음은 자전거 온라인 쇼핑몰의 제품 정보입니다. 이 제품에 대한 자연스럽고 매력적인 상세 설명을 200-300자 분량으로 작성해주세요.

제품명: {product.NameKorean} ({product.Name})
카테고리: {product.Category}
브랜드: {product.Brand}
가격: {product.Price:N0}원
기본 설명: {product.DescriptionKorean}
사양: {product.Specifications}

고객이 제품의 특징, 장점, 사용 용도를 쉽게 이해할 수 있도록 4-6문장으로 작성해주세요. 판매 문구가 아닌 정보 전달에 초점을 맞춰주세요.";

        var requestBody = new
        {
            model = LlmModel,
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = 0.7,
                top_p = 0.9
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            Console.WriteLine($"  [Ollama LLM] {product.NameKorean}에 대한 설명 생성 중...");
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (result.TryGetProperty("response", out var responseText))
            {
                return responseText.GetString()?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERROR] 설명 생성 실패: {ex.Message}");
            return product.DescriptionKorean; // 실패 시 기존 설명 사용
        }
    }

    /// <summary>
    /// 텍스트를 벡터 임베딩으로 변환합니다.
    /// </summary>
    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        var requestBody = new
        {
            model = EmbeddingModel,
            input = text
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/embed", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (result.TryGetProperty("embeddings", out var embeddings) && embeddings.GetArrayLength() > 0)
            {
                var embeddingArray = embeddings[0];
                var vectorList = new List<float>();

                foreach (var value in embeddingArray.EnumerateArray())
                {
                    vectorList.Add((float)value.GetDouble());
                }

                return vectorList.ToArray();
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ERROR] 임베딩 생성 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 제품 정보를 결합하여 검색용 텍스트를 생성합니다.
    /// </summary>
    public string BuildSearchableText(Product product)
    {
        var parts = new List<string>
        {
            product.NameKorean,
            product.Name,
            product.Category,
            product.Brand,
            product.DetailedDescription,
            product.DescriptionKorean,
            product.Specifications
        };

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
