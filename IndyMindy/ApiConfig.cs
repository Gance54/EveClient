using System;

namespace IndyMindy
{
    public static class ApiConfig
    {
        // API Configuration
        public const string BaseUrl = "http://localhost:5678";
        public const string SecureBaseUrl = "https://localhost:5679";
        public const string ApiBaseUrl = $"{BaseUrl}/api";
        public const string SecureApiBaseUrl = $"{SecureBaseUrl}/api";
        
        // Account endpoints
        public const string LoginEndpoint = $"{ApiBaseUrl}/account/login";
        public const string RegisterEndpoint = $"{ApiBaseUrl}/account/register";
        
        // Other API endpoints can be added here as needed
        // public const string UserProfileEndpoint = $"{ApiBaseUrl}/account/profile";
        // public const string RefreshTokenEndpoint = $"{ApiBaseUrl}/account/refresh";
    }
} 