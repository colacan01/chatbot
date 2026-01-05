using BicycleShopChatbot.Application.DTOs;

namespace BicycleShopChatbot.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, LoginResponse? Response)> RegisterAsync(
        RegisterRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<(bool Success, string? Error, LoginResponse? Response)> LoginAsync(
        LoginRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<(bool Success, string? Error, LoginResponse? Response)> RefreshTokenAsync(
        RefreshTokenRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<bool> RevokeRefreshTokenAsync(
        int userId, 
        CancellationToken cancellationToken = default);
}
