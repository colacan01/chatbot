using System.Security.Claims;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BicycleShopChatbot.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public async Task SendMessage(SendMessageRequest request)
    {
        try
        {
            // 인증된 사용자 ID 추출 및 강제 설정
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var authenticatedUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to SendMessage");
                await Clients.Caller.SendAsync("Error", "로그인이 필요합니다.");
                return;
            }

            // 요청의 UserId를 인증된 사용자로 강제 설정
            request.UserId = authenticatedUserId;

            var userNameClaim = Context.User?.FindFirst(ClaimTypes.Name);
            request.UserName = userNameClaim?.Value ?? "사용자";

            _logger.LogInformation(
                "Received message from user {UserId} in session {SessionId}: {Message}",
                authenticatedUserId,
                request.SessionId,
                request.Message.Substring(0, Math.Min(50, request.Message.Length)));

            var response = await _chatService.ProcessUserMessageAsync(request);

            await Clients.Caller.SendAsync("ReceiveMessage", response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message in ChatHub");
            await Clients.Caller.SendAsync("Error", "메시지 처리 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
        }
    }

    public async Task SendMessageStream(SendMessageRequest request)
    {
        try
        {
            // 인증된 사용자 ID 추출 및 강제 설정
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var authenticatedUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to SendMessageStream");
                await Clients.Caller.SendAsync("Error", "로그인이 필요합니다.");
                return;
            }

            // 요청의 UserId를 인증된 사용자로 강제 설정
            request.UserId = authenticatedUserId;

            var userNameClaim = Context.User?.FindFirst(ClaimTypes.Name);
            request.UserName = userNameClaim?.Value ?? "사용자";

            _logger.LogInformation(
                "Streaming message from user {UserId} in session {SessionId}: {Message}",
                authenticatedUserId,
                request.SessionId,
                request.Message.Substring(0, Math.Min(50, request.Message.Length)));

            await foreach (var chunk in _chatService.ProcessUserMessageStreamAsync(request))
            {
                // 각 청크를 클라이언트로 즉시 전송
                await Clients.Caller.SendAsync("ReceiveMessageChunk", chunk);
            }

            _logger.LogInformation("Streaming completed for session {SessionId}", request.SessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Streaming cancelled for session {SessionId}", request.SessionId);
            await Clients.Caller.SendAsync("StreamCancelled", request.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming in ChatHub");
            await Clients.Caller.SendAsync("StreamError", "스트리밍 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
        }
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation(
            "Client {ConnectionId} joined session {SessionId}",
            Context.ConnectionId,
            sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation(
            "Client {ConnectionId} left session {SessionId}",
            Context.ConnectionId,
            sessionId);
    }

    public async Task LoadSessionHistory(string sessionId)
    {
        try
        {
            // 인증된 사용자 ID 추출
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Unauthorized access attempt to LoadSessionHistory");
                await Clients.Caller.SendAsync("Error", "로그인이 필요합니다.");
                return;
            }

            _logger.LogInformation(
                "Loading session history for user {UserId}, session {SessionId}",
                userId,
                sessionId);

            var sessionDto = await _chatService.LoadSessionHistoryAsync(sessionId, userId);
            await Clients.Caller.SendAsync("SessionHistoryLoaded", sessionDto);

            _logger.LogInformation(
                "Session history loaded successfully: {SessionId}, {MessageCount} messages",
                sessionId,
                sessionDto.RecentMessages.Count);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to session {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "세션에 접근할 수 없습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session history for {SessionId}", sessionId);
            await Clients.Caller.SendAsync("Error", "세션 히스토리 로드 중 오류가 발생했습니다.");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
