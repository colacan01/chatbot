using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BicycleShopChatbot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _hostEnvironment;

    public AuthController(
        IAuthService authService, 
        ILogger<AuthController> logger,
        IWebHostEnvironment hostEnvironment)
    {
        _authService = authService;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogWarning("⚠️ 회원가입 유효성 검증 실패: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }
            return BadRequest(ModelState);
        }

        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("🔵 회원가입 요청: Email={Email}, UserName={UserName}", request.Email, request.UserName);
        }
        
        try
        {
            var (success, error, response) = await _authService.RegisterAsync(request);
            
            if (!success)
            {
                if (_hostEnvironment.IsDevelopment())
                {
                    _logger.LogWarning("⚠️ 회원가입 실패: {Error}", error);
                }
                return BadRequest(new { message = error });
            }

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("✅ 회원가입 성공: {Email}", request.Email);
            }
            return Ok(response);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "❌ 데이터베이스 저장 실패: {Message}", ex.Message);
            return StatusCode(500, new { message = "데이터베이스 저장 중 오류가 발생했습니다." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 회원가입 중 예외 발생: {Message}", ex.Message);
            return StatusCode(500, new { message = "서버 오류가 발생했습니다." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogWarning("⚠️ 로그인 유효성 검증 실패: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }
            return BadRequest(ModelState);
        }

        if (_hostEnvironment.IsDevelopment())
        {
            _logger.LogInformation("🔵 로그인 요청: Email={Email}", request.Email);
        }

        try
        {
            var (success, error, response) = await _authService.LoginAsync(request);
            
            if (!success)
            {
                if (_hostEnvironment.IsDevelopment())
                {
                    _logger.LogWarning("⚠️ 로그인 실패: {Error}", error);
                }
                return Unauthorized(new { message = error });
            }

            if (_hostEnvironment.IsDevelopment())
            {
                _logger.LogInformation("✅ 로그인 성공: {Email}", request.Email);
            }
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 로그인 중 예외 발생: {Message}", ex.Message);
            return StatusCode(500, new { message = "서버 오류가 발생했습니다." });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var (success, error, response) = await _authService.RefreshTokenAsync(request);
        
        if (!success)
        {
            return Unauthorized(new { message = error });
        }

        return Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var success = await _authService.RevokeRefreshTokenAsync(userId);
        
        if (!success)
        {
            return BadRequest(new { message = "로그아웃 처리에 실패했습니다." });
        }

        return Ok(new { message = "로그아웃되었습니다." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var emailClaim = User.FindFirst(ClaimTypes.Email);
        var nameClaim = User.FindFirst(ClaimTypes.Name);
        var roleClaim = User.FindFirst(ClaimTypes.Role);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        return Ok(new
        {
            userId,
            email = emailClaim?.Value,
            userName = nameClaim?.Value,
            role = roleClaim?.Value
        });
    }
}
