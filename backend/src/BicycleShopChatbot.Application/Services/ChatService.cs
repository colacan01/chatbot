using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Configuration;
using BicycleShopChatbot.Application.Plugins;
using BicycleShopChatbot.Domain.Entities;
using BicycleShopChatbot.Domain.Enums;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace BicycleShopChatbot.Application.Services;

public class ChatService : IChatService
{
    private readonly IOllamaService _ollamaService;
    private readonly IPromptService _promptService;
    private readonly IProductContextService _productContextService;
    private readonly IOrderContextService _orderContextService;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IFAQRepository _faqRepository;
    private readonly ProductSearchPlugin _productSearchPlugin;
    private readonly FaqSearchPlugin _faqSearchPlugin;
    private readonly SemanticKernelSettings _skSettings;
    private readonly IResponseValidationService _responseValidationService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IOllamaService ollamaService,
        IPromptService promptService,
        IProductContextService productContextService,
        IOrderContextService orderContextService,
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IFAQRepository faqRepository,
        ProductSearchPlugin productSearchPlugin,
        FaqSearchPlugin faqSearchPlugin,
        SemanticKernelSettings skSettings,
        IResponseValidationService responseValidationService,
        ILogger<ChatService> logger)
    {
        _ollamaService = ollamaService;
        _promptService = promptService;
        _productContextService = productContextService;
        _orderContextService = orderContextService;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _faqRepository = faqRepository;
        _productSearchPlugin = productSearchPlugin;
        _faqSearchPlugin = faqSearchPlugin;
        _skSettings = skSettings;
        _responseValidationService = responseValidationService;
        _logger = logger;
    }

    public async Task<ChatMessageDto> ProcessUserMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var session = await GetOrCreateSessionAsync(
            request.SessionId,
            request.UserId,
            request.UserName,
            cancellationToken);

        // 첫 메시지인 경우 세션 제목 자동 생성
        if (session.TotalMessages == 0 && string.IsNullOrEmpty(session.Title))
        {
            var title = request.Message.Length > 50
                ? request.Message.Substring(0, 50) + "..."
                : request.Message;
            await _sessionRepository.UpdateSessionTitleAsync(session.Id, title, cancellationToken);
            _logger.LogInformation("Auto-generated session title: {Title}", title);
        }

        var userMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = MessageRole.User,
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(userMessage, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        var conversationHistory = await GetConversationHistoryAsync(
            request.SessionId,
            20,
            cancellationToken);

        var intent = _promptService.DetectIntent(request.Message);
        _logger.LogInformation("Detected intent: {Intent} for message: {Message}",
            intent, request.Message.Substring(0, Math.Min(50, request.Message.Length)));

        var temperature = _promptService.GetTemperatureForCategory(intent);
        _logger.LogInformation("Using temperature {Temperature} for category {Category}", temperature, intent);

        var systemPrompt = await BuildContextualPromptAsync(intent, request.Message, cancellationToken);

        var aiResponse = await _ollamaService.GenerateResponseAsync(
            request.Message,
            conversationHistory,
            systemPrompt,
            temperature,
            cancellationToken);

        // Validate and clean response for product-related intents
        if (intent == ChatCategory.ProductSearch || intent == ChatCategory.ProductDetails)
        {
            var validationResult = await _responseValidationService.ValidateAndCleanResponseAsync(
                aiResponse, cancellationToken);

            if (validationResult.HasModifications)
            {
                _logger.LogWarning(
                    "Response validation removed {Count} invalid product codes: {Codes}",
                    validationResult.InvalidCodes.Count,
                    string.Join(", ", validationResult.InvalidCodes));
            }

            aiResponse = validationResult.CleanedResponse;
        }

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = MessageRole.Assistant,
            Content = aiResponse,
            Timestamp = DateTime.UtcNow,
            Category = intent,
            IntentDetected = intent.ToString(),
            ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
        };

        await _messageRepository.AddAsync(assistantMessage, cancellationToken);

        var sessionEntity = await _sessionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (sessionEntity != null)
        {
            sessionEntity.LastActivityAt = DateTime.UtcNow;
            sessionEntity.TotalMessages += 2;
            _sessionRepository.Update(sessionEntity);
        }

        await _sessionRepository.SaveChangesAsync(cancellationToken);

        // 사용자의 세션이 30개를 초과하면 오래된 세션 자동 삭제
        if (request.UserId.HasValue)
        {
            await _sessionRepository.DeleteOldUserSessionsAsync(request.UserId.Value, 30, cancellationToken);
        }

        stopwatch.Stop();
        _logger.LogInformation("Processed message in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        return new ChatMessageDto
        {
            Id = assistantMessage.Id,
            SessionId = request.SessionId,
            Role = MessageRole.Assistant.ToString(),
            Content = aiResponse,
            Timestamp = assistantMessage.Timestamp,
            Category = intent.ToString()
        };
    }

    public async IAsyncEnumerable<ChatStreamChunk> ProcessUserMessageStreamAsync(
        SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. 세션 생성/조회 및 사용자 메시지 저장
        var session = await GetOrCreateSessionAsync(
            request.SessionId,
            request.UserId,
            request.UserName,
            cancellationToken);

        // 첫 메시지인 경우 세션 제목 자동 생성
        if (session.TotalMessages == 0 && string.IsNullOrEmpty(session.Title))
        {
            var title = request.Message.Length > 50
                ? request.Message.Substring(0, 50) + "..."
                : request.Message;
            await _sessionRepository.UpdateSessionTitleAsync(session.Id, title, cancellationToken);
            _logger.LogInformation("Auto-generated session title for streaming: {Title}", title);
        }

        var userMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = MessageRole.User,
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };

        await _messageRepository.AddAsync(userMessage, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);

        // 2. 대화 히스토리 및 Intent 감지
        var conversationHistory = await GetConversationHistoryAsync(
            request.SessionId,
            20,
            cancellationToken);

        var intent = _promptService.DetectIntent(request.Message);
        _logger.LogInformation("Detected intent: {Intent} for streaming message", intent);

        var temperature = _promptService.GetTemperatureForCategory(intent);
        _logger.LogInformation("Using temperature {Temperature} for category {Category}", temperature, intent);

        var systemPrompt = await BuildContextualPromptAsync(intent, request.Message, cancellationToken);

        // 3. 스트리밍 시작
        var messageId = Guid.NewGuid().ToString();
        var fullContent = new StringBuilder();

        await foreach (var chunk in _ollamaService.GenerateResponseStreamAsync(
            request.Message,
            conversationHistory,
            systemPrompt,
            temperature,
            cancellationToken))
        {
            fullContent.Append(chunk);

            yield return new ChatStreamChunk
            {
                SessionId = request.SessionId,
                MessageId = messageId,
                Content = chunk,
                IsComplete = false,
                Timestamp = DateTime.UtcNow,
                Category = intent.ToString()
            };
        }

        // 4. 스트리밍 완료 후 응답 검증 및 DB 저장
        stopwatch.Stop();

        var finalContent = fullContent.ToString();

        // Validate and clean response for product-related intents
        if (intent == ChatCategory.ProductSearch || intent == ChatCategory.ProductDetails)
        {
            var validationResult = await _responseValidationService.ValidateAndCleanResponseAsync(
                finalContent, cancellationToken);

            if (validationResult.HasModifications)
            {
                _logger.LogWarning(
                    "Streaming response validation found {Count} invalid product codes: {Codes}",
                    validationResult.InvalidCodes.Count,
                    string.Join(", ", validationResult.InvalidCodes));

                // 수정 청크 전송 - 사용자에게 잘못된 제품 정보 알림
                var correctionMessage = $"\n\n---\n⚠️ **수정 안내**: 위 응답에서 언급된 일부 제품({string.Join(", ", validationResult.InvalidCodes)})은 " +
                    "현재 판매 목록에 없는 제품입니다. 해당 내용을 무시해 주세요.";

                yield return new ChatStreamChunk
                {
                    SessionId = request.SessionId,
                    MessageId = messageId,
                    Content = correctionMessage,
                    IsComplete = false,
                    Timestamp = DateTime.UtcNow,
                    Category = intent.ToString()
                };

                finalContent += correctionMessage;
            }
        }

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = MessageRole.Assistant,
            Content = finalContent,
            Timestamp = DateTime.UtcNow,
            Category = intent,
            IntentDetected = intent.ToString(),
            ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds
        };

        await _messageRepository.AddAsync(assistantMessage, cancellationToken);

        // 5. 세션 업데이트
        var sessionEntity = await _sessionRepository.GetBySessionIdAsync(request.SessionId, cancellationToken);
        if (sessionEntity != null)
        {
            sessionEntity.LastActivityAt = DateTime.UtcNow;
            sessionEntity.TotalMessages += 2;
            _sessionRepository.Update(sessionEntity);
        }

        await _sessionRepository.SaveChangesAsync(cancellationToken);

        // 사용자의 세션이 30개를 초과하면 오래된 세션 자동 삭제
        if (request.UserId.HasValue)
        {
            await _sessionRepository.DeleteOldUserSessionsAsync(request.UserId.Value, 30, cancellationToken);
        }

        // 6. 완료 청크 전송
        yield return new ChatStreamChunk
        {
            SessionId = request.SessionId,
            MessageId = messageId,
            Content = string.Empty,
            IsComplete = true,
            Timestamp = DateTime.UtcNow,
            Category = intent.ToString()
        };

        _logger.LogInformation(
            "Streaming completed in {ElapsedMs}ms, total {Length} chars",
            stopwatch.ElapsedMilliseconds,
            fullContent.Length);
    }

    public async Task<List<ChatMessageDto>> GetConversationHistoryAsync(
        string sessionId,
        int messageCount = 50,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            return new List<ChatMessageDto>();
        }

        var messages = await _messageRepository.GetMessagesBySessionIdAsync(
            session.Id,
            messageCount,
            cancellationToken);

        return messages.Select(m => new ChatMessageDto
        {
            Id = m.Id,
            SessionId = sessionId,
            Role = m.Role.ToString(),
            Content = m.Content,
            Timestamp = m.Timestamp,
            Category = m.Category?.ToString()
        }).ToList();
    }

    public async Task<ChatSessionDto> GetOrCreateSessionAsync(
        string sessionId,
        int? userId = null,
        string? userName = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);

        if (session == null)
        {
            session = new ChatSession
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                UserName = userName,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                TotalMessages = 0
            };

            await _sessionRepository.AddAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new chat session: {SessionId}", sessionId);
        }

        return new ChatSessionDto
        {
            Id = session.Id,
            SessionId = session.SessionId,
            UserId = session.UserId,
            UserName = session.UserName,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            IsActive = session.IsActive,
            TotalMessages = session.TotalMessages
        };
    }

    private async Task<string> BuildContextualPromptAsync(
        ChatCategory intent,
        string userMessage,
        CancellationToken cancellationToken)
    {
        return intent switch
        {
            ChatCategory.ProductSearch => await BuildProductSearchPromptAsync(userMessage, cancellationToken),
            ChatCategory.ProductDetails => await BuildProductSearchPromptAsync(userMessage, cancellationToken),
            ChatCategory.OrderStatus => await BuildOrderStatusPromptAsync(userMessage, cancellationToken),
            ChatCategory.FAQ => await BuildFaqPromptAsync(userMessage, cancellationToken),
            _ => _promptService.GetSystemPrompt(intent)
        };
    }

    private async Task<string> BuildProductSearchPromptAsync(
        string userMessage,
        CancellationToken cancellationToken)
    {
        // Use Semantic Kernel plugin if enabled
        if (_skSettings.Enabled)
        {
            try
            {
                _logger.LogInformation("Using Semantic Kernel ProductSearchPlugin for query: {Query}", userMessage);

                var productContext = await _productSearchPlugin.SearchProductsAsync(userMessage, cancellationToken);

                if (!string.IsNullOrWhiteSpace(productContext) && !productContext.Contains("검색 결과가 없습니다"))
                {
                    return _promptService.GetSystemPrompt(ChatCategory.ProductSearch) + "\n\n" +
                           "========================================\n" +
                           "[ 검색된 제품 정보 (재순위 결과) ]\n" +
                           "========================================\n" +
                           productContext;
                }
                else
                {
                    _logger.LogWarning("SK plugin returned no products, falling back to legacy search");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using SK ProductSearchPlugin, falling back to legacy search");
            }
        }

        // Fallback to existing implementation
        // Step 1: Extract ALL filters from user message
        var productName = _promptService.ExtractProductName(userMessage);
        var (minPrice, maxPrice) = _promptService.ExtractPriceRange(userMessage);
        var category = _promptService.ExtractProductCategory(userMessage);

        // Step 2: Determine if ANY filters are present
        bool hasProductName = !string.IsNullOrEmpty(productName);
        bool hasPriceFilter = minPrice.HasValue || maxPrice.HasValue;
        bool hasCategoryFilter = !string.IsNullOrEmpty(category);
        bool hasAnyFilter = hasProductName || hasPriceFilter || hasCategoryFilter;

        List<Product> products;

        // Step 3: Apply filters
        if (hasAnyFilter)
        {
            // Log extracted filters
            var filterDescription = new List<string>();
            if (hasProductName) filterDescription.Add($"Product: {productName}");
            if (hasPriceFilter) filterDescription.Add($"Price: {minPrice?.ToString("N0") ?? "min"}~{maxPrice?.ToString("N0") ?? "max"}");
            if (hasCategoryFilter) filterDescription.Add($"Category: {category}");

            _logger.LogInformation(
                "Applying combined filters: {Filters}",
                string.Join(", ", filterDescription));

            // Apply combined filter search
            products = await _productContextService.SearchWithFiltersAsync(
                minPrice,
                maxPrice,
                category,
                productName,
                cancellationToken);

            _logger.LogInformation(
                "Combined filter search returned {Count} products",
                products.Count);

            if (products.Any())
            {
                _logger.LogInformation(
                    "Top products: {Products}",
                    string.Join(", ", products.Take(3).Select(p => p.NameKorean)));

                return _promptService.GetProductSearchPrompt(userMessage, products);
            }
            else
            {
                // No products match the combined filters
                _logger.LogWarning(
                    "No products found matching filters: {Filters}",
                    string.Join(", ", filterDescription));

                return _promptService.GetNoProductsFoundPrompt(userMessage);
            }
        }

        // Step 4: No filters detected, use vector search
        _logger.LogInformation("No specific filters requested, using vector search");

        products = await _productContextService.SearchProductsAsync(
            userMessage,
            5,
            cancellationToken);

        _logger.LogInformation(
            "Vector search for '{Query}' returned {Count} products",
            userMessage.Length > 50 ? userMessage.Substring(0, 50) + "..." : userMessage,
            products.Count);

        if (products.Any())
        {
            _logger.LogInformation(
                "Top products: {Products}",
                string.Join(", ", products.Take(3).Select(p => p.NameKorean)));

            return _promptService.GetProductSearchPrompt(userMessage, products);
        }

        // Final fallback: No products prompt
        _logger.LogWarning("All search methods failed, using no-products prompt");
        return _promptService.GetNoProductsFoundPrompt(userMessage);
    }

    private async Task<string> BuildOrderStatusPromptAsync(
        string userMessage,
        CancellationToken cancellationToken)
    {
        var orderNumber = ExtractOrderNumber(userMessage);
        Order? order = null;

        if (!string.IsNullOrEmpty(orderNumber))
        {
            order = await _orderContextService.GetOrderByNumberAsync(orderNumber, cancellationToken);
        }

        return _promptService.GetOrderStatusPrompt(orderNumber ?? userMessage, order);
    }

    private async Task<string> BuildFaqPromptAsync(
        string userMessage,
        CancellationToken cancellationToken)
    {
        // Use Semantic Kernel plugin if enabled
        if (_skSettings.Enabled)
        {
            try
            {
                _logger.LogInformation("Using Semantic Kernel FaqSearchPlugin for question: {Question}", userMessage);

                var faqContext = await _faqSearchPlugin.SearchFaqsAsync(userMessage, cancellationToken);

                if (!string.IsNullOrWhiteSpace(faqContext) && !faqContext.Contains("관련 FAQ를 찾을 수 없습니다"))
                {
                    return _promptService.GetSystemPrompt(ChatCategory.FAQ) + "\n\n" +
                           "========================================\n" +
                           "[ 관련 FAQ (재순위 결과) ]\n" +
                           "========================================\n" +
                           faqContext;
                }
                else
                {
                    _logger.LogWarning("SK plugin returned no FAQs, falling back to legacy search");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using SK FaqSearchPlugin, falling back to legacy search");
            }
        }

        // Fallback to existing implementation
        var faqs = await _faqRepository.SearchFAQsAsync(userMessage, 5, cancellationToken);
        return _promptService.GetFaqPrompt(userMessage, faqs);
    }

    private string? ExtractOrderNumber(string message)
    {
        var match = Regex.Match(message, @"\b([A-Z0-9]{6,})\b");
        return match.Success ? match.Value : null;
    }

    // Session management methods
    public async Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepository.GetUserSessionsAsync(userId, 30, cancellationToken);

        return sessions.Select(s => new ChatSessionDto
        {
            Id = s.Id,
            SessionId = s.SessionId,
            UserId = s.UserId,
            UserName = s.UserName,
            Title = s.Title ?? "새 대화",
            CreatedAt = s.CreatedAt,
            LastActivityAt = s.LastActivityAt,
            IsActive = s.IsActive,
            TotalMessages = s.TotalMessages,
            LastMessagePreview = s.Messages?.OrderByDescending(m => m.Timestamp)
                .FirstOrDefault()?.Content?.Substring(0, Math.Min(100, s.Messages.FirstOrDefault()?.Content?.Length ?? 0))
        });
    }

    public async Task<ChatSessionDto> LoadSessionHistoryAsync(
        string sessionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);

        // 세션이 없으면 새 세션 정보 반환 (에러 대신)
        if (session == null)
        {
            _logger.LogInformation("Session {SessionId} not found, returning empty session for user {UserId}", sessionId, userId);
            return new ChatSessionDto
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                UserId = userId,
                UserName = null,
                Title = null,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                TotalMessages = 0,
                RecentMessages = new List<ChatMessageDto>()
            };
        }

        // 세션 소유자가 다르면 접근 거부
        if (session.UserId != userId)
        {
            throw new UnauthorizedAccessException("세션에 접근할 수 없습니다.");
        }

        var messages = await _messageRepository.GetMessagesBySessionIdAsync(session.Id, 100, cancellationToken);

        return new ChatSessionDto
        {
            Id = session.Id,
            SessionId = session.SessionId,
            UserId = session.UserId,
            UserName = session.UserName,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.LastActivityAt,
            IsActive = session.IsActive,
            TotalMessages = session.TotalMessages,
            RecentMessages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SessionId = session.SessionId,
                Role = m.Role.ToString(),
                Content = m.Content,
                Timestamp = m.Timestamp,
                Category = m.Category?.ToString()
            }).ToList()
        };
    }

    public async Task<bool> DeleteSessionAsync(
        string sessionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetBySessionIdAsync(sessionId, cancellationToken);

        if (session == null || session.UserId != userId)
        {
            return false;
        }

        _sessionRepository.Remove(session);
        await _sessionRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted session: {SessionId} for user: {UserId}", sessionId, userId);
        return true;
    }
}
