using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
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
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IOllamaService ollamaService,
        IPromptService promptService,
        IProductContextService productContextService,
        IOrderContextService orderContextService,
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IFAQRepository faqRepository,
        ILogger<ChatService> logger)
    {
        _ollamaService = ollamaService;
        _promptService = promptService;
        _productContextService = productContextService;
        _orderContextService = orderContextService;
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _faqRepository = faqRepository;
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

        var systemPrompt = await BuildContextualPromptAsync(intent, request.Message, cancellationToken);

        var aiResponse = await _ollamaService.GenerateResponseAsync(
            request.Message,
            conversationHistory,
            systemPrompt,
            cancellationToken);

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

        var systemPrompt = await BuildContextualPromptAsync(intent, request.Message, cancellationToken);

        // 3. 스트리밍 시작
        var messageId = Guid.NewGuid().ToString();
        var fullContent = new StringBuilder();

        await foreach (var chunk in _ollamaService.GenerateResponseStreamAsync(
            request.Message,
            conversationHistory,
            systemPrompt,
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

        // 4. 스트리밍 완료 후 DB 저장
        stopwatch.Stop();

        var assistantMessage = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = MessageRole.Assistant,
            Content = fullContent.ToString(),
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
            ChatCategory.OrderStatus => await BuildOrderStatusPromptAsync(userMessage, cancellationToken),
            ChatCategory.FAQ => await BuildFaqPromptAsync(userMessage, cancellationToken),
            _ => _promptService.GetSystemPrompt(intent)
        };
    }

    private async Task<string> BuildProductSearchPromptAsync(
        string userMessage,
        CancellationToken cancellationToken)
    {
        var products = await _productContextService.SearchProductsAsync(
            userMessage,
            5,
            cancellationToken);

        if (!products.Any())
        {
            products = await _productContextService.SearchProductsAsync("", 5, cancellationToken);
        }

        return _promptService.GetProductSearchPrompt(userMessage, products);
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
