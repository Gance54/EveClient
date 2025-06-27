using System.Threading.Tasks;
using System;

namespace IndyMindy.Services
{
    public interface IAccountService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<TokenVerificationResponse> VerifyTokenAsync(string token);
        Task<UserInfo> GetUserAsync(int userId, string token);
    }

    public class AccountService : IAccountService
    {
        private readonly IHttpService _httpService;

        public AccountService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            return await _httpService.PostAsync<LoginResponse>("/api/account/login", request);
        }

        public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            return await _httpService.PostAsync<RegisterResponse>("/api/account/register", request);
        }

        public async Task<TokenVerificationResponse> VerifyTokenAsync(string token)
        {
            var request = new { Token = token };
            return await _httpService.PostAsync<TokenVerificationResponse>("/api/account/verify-token", request);
        }

        public async Task<UserInfo> GetUserAsync(int userId, string token)
        {
            var request = new { UserId = userId, Token = token };
            return await _httpService.PostAsync<UserInfo>("/api/account/get-user", request);
        }
    }

    // Request/Response models
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public UserInfo User { get; set; }
    }

    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class RegisterResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
    }

    public class TokenVerificationResponse
    {
        public int UserId { get; set; }
    }

    public class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public bool IsSubscribed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 