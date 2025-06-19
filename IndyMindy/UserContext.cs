using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace IndyMindy
{
    public class TokenInfo
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public DateTime IssuedAt { get; set; }

        public DateTime ExpiresAt => IssuedAt.AddSeconds(ExpiresIn);
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        // Constructor for backend response
        public TokenInfo(string access_token, string refresh_token, int expires_in, string token_type, DateTime issued_at)
        {
            AccessToken = access_token;
            RefreshToken = refresh_token;
            ExpiresIn = expires_in;
            TokenType = token_type;
            IssuedAt = issued_at;
        }

        // Optional: static factory for deserialization from backend JSON
        public static TokenInfo FromBackendResponse(dynamic response)
        {
            return new TokenInfo(
                (string)response.access_token,
                (string)response.refresh_token,
                (int)response.expires_in,
                (string)response.token_type,
                (DateTime)response.issued_at
            );
        }

        public TokenInfo() { } // Parameterless for serialization
    }

    public class UserContext
    {
        public string Email { get; set; }
        public string AuthToken { get; set; }
        public bool IsSubscribed { get; set; }
        public List<EveCharacterContext> Characters { get; set; } = new();
        public ProductionPlanner Planner { get; set; } = new();
        public TokenInfo Tokens { get; set; }
    }

    public static class SessionManager
    {
        public static UserContext CurrentUser { get; set; }
    }

    public static class SessionPersistence
    {
        private static readonly string SessionFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IndyMindy",
            "session.json"
        );

        public static void SaveSession(UserContext user)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(SessionFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                var json = JsonSerializer.Serialize(user, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(SessionFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
            }
        }

        public static UserContext LoadSession()
        {
            try
            {
                if (!File.Exists(SessionFilePath))
                    return null;

                var json = File.ReadAllText(SessionFilePath);
                var user = JsonSerializer.Deserialize<UserContext>(json);
                return user;
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
                return null;
            }
        }

        public static void ClearSession()
        {
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    File.Delete(SessionFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log error or handle as needed
                System.Diagnostics.Debug.WriteLine($"Failed to clear session: {ex.Message}");
            }
        }
    }
}
