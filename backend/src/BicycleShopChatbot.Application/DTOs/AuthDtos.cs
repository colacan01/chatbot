using System.ComponentModel.DataAnnotations;

namespace BicycleShopChatbot.Application.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "이메일은 필수 항목입니다.")]
    [EmailAddress(ErrorMessage = "유효한 이메일 주소를 입력해주세요.")]
    [StringLength(200, ErrorMessage = "이메일은 최대 200자까지 입력 가능합니다.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "사용자명은 필수 항목입니다.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "사용자명은 3자 이상 100자 이하여야 합니다.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호는 필수 항목입니다.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "비밀번호는 8자 이상 100자 이하여야 합니다.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호 확인은 필수 항목입니다.")]
    [Compare(nameof(Password), ErrorMessage = "비밀번호가 일치하지 않습니다.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "이메일은 필수 항목입니다.")]
    [EmailAddress(ErrorMessage = "유효한 이메일 주소를 입력해주세요.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "비밀번호는 필수 항목입니다.")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
