using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BicycleShopChatbot.Api.Hubs;

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
            _logger.LogInformation(
                "Received message from session {SessionId}: {Message}",
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
            _logger.LogInformation(
                "Streaming message from session {SessionId}: {Message}",
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
