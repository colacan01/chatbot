using System.Text.RegularExpressions;
using BicycleShopChatbot.Application.DTOs;
using BicycleShopChatbot.Application.Interfaces;
using BicycleShopChatbot.Application.Interfaces.Repositories;
using BicycleShopChatbot.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BicycleShopChatbot.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 유효성 검증
            var validationError = ValidateRegistration(request);
            if (validationError != null)
            {
                return (false, validationError, null);
            }

            // 이메일 중복 확인
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            {
                return (false, "이미 등록된 이메일 주소입니다.", null);
            }

            // 사용자명 중복 확인
            if (await _userRepository.UserNameExistsAsync(request.UserName, cancellationToken))
            {
                return (false, "이미 사용 중인 사용자명입니다.", null);
            }

            // 비밀번호 해싱
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // 사용자 생성
            var user = new User
            {
                Email = request.Email.ToLower(),
                UserName = request.UserName,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New user registered: {Email}", request.Email);

            // 토큰 생성 및 로그인 응답 반환
            var loginResponse = await GenerateLoginResponse(user, cancellationToken);
            return (true, null, loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return (false, "회원가입 처리 중 오류가 발생했습니다.", null);
        }
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 사용자 조회
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);
            }

            // 계정 활성화 확인
            if (!user.IsActive)
            {
                return (false, "비활성화된 계정입니다.", null);
            }

            // 비밀번호 검증
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return (false, "이메일 또는 비밀번호가 올바르지 않습니다.", null);
            }

            // 마지막 로그인 시간 업데이트
            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User logged in: {Email}", request.Email);

            // 토큰 생성 및 응답 반환
            var loginResponse = await GenerateLoginResponse(user, cancellationToken);
            return (true, null, loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return (false, "로그인 처리 중 오류가 발생했습니다.", null);
        }
    }

    public async Task<(bool Success, string? Error, LoginResponse? Response)> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 만료된 액세스 토큰에서 사용자 정보 추출
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return (false, "유효하지 않은 액세스 토큰입니다.", null);
            }

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return (false, "유효하지 않은 토큰입니다.", null);
            }

            // 리프레시 토큰으로 사용자 조회
            var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (user == null || user.Id != userId)
            {
                return (false, "유효하지 않은 리프레시 토큰입니다.", null);
            }

            // 리프레시 토큰 만료 확인
            if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return (false, "만료된 리프레시 토큰입니다.", null);
            }

            // 새 토큰 생성
            var loginResponse = await GenerateLoginResponse(user, cancellationToken);
            
            _logger.LogInformation("Token refreshed for user: {UserId}", userId);
            
            return (true, null, loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return (false, "토큰 갱신 중 오류가 발생했습니다.", null);
        }
    }

    public async Task<bool> RevokeRefreshTokenAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token revoked for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking refresh token for user: {UserId}", userId);
            return false;
        }
    }

    private async Task<LoginResponse> GenerateLoginResponse(User user, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };
    }

    private string? ValidateRegistration(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return "이메일은 필수 항목입니다.";
        }

        if (!IsValidEmail(request.Email))
        {
            return "유효한 이메일 주소를 입력해주세요.";
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return "사용자명은 필수 항목입니다.";
        }

        if (request.UserName.Length < 3 || request.UserName.Length > 100)
        {
            return "사용자명은 3자 이상 100자 이하여야 합니다.";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "비밀번호는 필수 항목입니다.";
        }

        if (request.Password.Length < 8)
        {
            return "비밀번호는 최소 8자 이상이어야 합니다.";
        }

        if (request.Password != request.ConfirmPassword)
        {
            return "비밀번호가 일치하지 않습니다.";
        }

        return null;
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
}
