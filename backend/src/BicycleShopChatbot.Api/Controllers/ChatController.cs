using System.Security.Claims;
using BicycleShopChatbot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BicycleShopChatbot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// 사용자의 세션 목록 조회 (최근 30개)
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetUserSessions(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Unauthorized access to GetUserSessions");
            return Unauthorized(new { message = "로그인이 필요합니다." });
        }

        try
        {
            var sessions = await _chatService.GetUserSessionsAsync(userId, cancellationToken);

            _logger.LogInformation("Retrieved {Count} sessions for user {UserId}",
                sessions.Count(), userId);

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}", userId);
            return StatusCode(500, new { message = "세션 목록을 불러오는 중 오류가 발생했습니다." });
        }
    }

    /// <summary>
    /// 특정 세션의 히스토리 조회
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSessionHistory(string sessionId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Unauthorized access to GetSessionHistory for session {SessionId}", sessionId);
            return Unauthorized(new { message = "로그인이 필요합니다." });
        }

        try
        {
            var session = await _chatService.LoadSessionHistoryAsync(sessionId, userId, cancellationToken);

            _logger.LogInformation("Retrieved session {SessionId} for user {UserId}", sessionId, userId);

            return Ok(session);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("User {UserId} attempted to access unauthorized session {SessionId}",
                userId, sessionId);
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId} for user {UserId}",
                sessionId, userId);
            return NotFound(new { message = "세션을 찾을 수 없습니다." });
        }
    }

    /// <summary>
    /// 세션 삭제
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            _logger.LogWarning("Unauthorized access to DeleteSession for session {SessionId}", sessionId);
            return Unauthorized(new { message = "로그인이 필요합니다." });
        }

        try
        {
            var success = await _chatService.DeleteSessionAsync(sessionId, userId, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Deleted session {SessionId} for user {UserId}", sessionId, userId);
                return Ok(new { message = "세션이 삭제되었습니다." });
            }
            else
            {
                _logger.LogWarning("User {UserId} attempted to delete non-existent or unauthorized session {SessionId}",
                    userId, sessionId);
                return NotFound(new { message = "세션을 찾을 수 없거나 삭제할 권한이 없습니다." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId} for user {UserId}",
                sessionId, userId);
            return StatusCode(500, new { message = "세션 삭제 중 오류가 발생했습니다." });
        }
    }
}
