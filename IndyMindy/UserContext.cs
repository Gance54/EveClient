using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using IndyMindy.Services;

namespace IndyMindy
{
    public class TokenInfo
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }

        // Constructor for backend response
        public TokenInfo(string access_token, string refresh_token, string token_type)
        {
            AccessToken = access_token;
            RefreshToken = refresh_token;
            TokenType = token_type;
        }

        // Optional: static factory for deserialization from backend JSON
        public static TokenInfo FromBackendResponse(dynamic response)
        {
            return new TokenInfo(
                (string)response.access_token,
                (string)response.refresh_token,
                (string)response.token_type
            );
        }

        public TokenInfo() { } // Parameterless for serialization
    }

    public class UserContext
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string AuthToken { get; set; }
        public bool IsSubscribed { get; set; }
        public List<EveCharacterContext> Characters { get; set; } = new();
        public ProductionPlanner Planner { get; set; } = new();
        public TokenInfo Tokens { get; set; }

        public bool IsLoggedIn => Tokens != null;
        public bool IsActiveSubscriber => IsLoggedIn && IsSubscribed;
    }

    public class SessionFileData
    {
        public int UserId { get; set; }
        public TokenInfo Tokens { get; set; }
    }

    public static class SessionManager
    {
        private static UserContext _currentUser;
        private static readonly IAccountService _accountService;
        
        static SessionManager()
        {
            var httpService = new HttpService();
            _accountService = new AccountService(httpService);
        }
        
        public static UserContext CurrentUser 
        { 
            get => _currentUser;
            set
            {
                _currentUser = value;
                if (_currentUser != null)
                {
                    SessionPersistence.SaveSession(_currentUser);
                }
            }
        }

        public static async Task<bool> VerifyTokenAsync()
        {
            if (CurrentUser?.Tokens?.AccessToken == null)
                return false;

            try
            {
                var userInfo = await _accountService.VerifyTokenAsync(CurrentUser.Tokens.AccessToken);
                return userInfo != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsSessionValid()
        {
            if (CurrentUser?.Tokens == null)
                return false;

            // Check if the main user token is expired
            // TODO: send request to server to validate the token
            
            // Check if any character tokens are expired
            if (CurrentUser.Characters != null)
            {
                foreach (var character in CurrentUser.Characters)
                {
                    if (character.TokenExpiration <= DateTime.UtcNow)
                        return false;
                }
            }

            return true;
        }

        public static void ValidateAndCleanSession()
        {
            if (!IsSessionValid())
            {
                // Clear expired session
                ClearSession();
            }
        }

        public static void ClearSession()
        {
            _currentUser = null;
            SessionPersistence.ClearSession();
        }

        public static void AddCharacter(EveCharacterContext character)
        {
            if (CurrentUser != null)
            {
                CurrentUser.Characters.Add(character);
                // Session is automatically saved via the CurrentUser property setter
                // when we reassign it (even though it's the same object)
                CurrentUser = CurrentUser; // Trigger save
            }
        }

        public static void RemoveCharacter(EveCharacterContext character)
        {
            if (CurrentUser != null)
            {
                CurrentUser.Characters.Remove(character);
                // Session is automatically saved via the CurrentUser property setter
                CurrentUser = CurrentUser; // Trigger save
            }
        }

        public static void UpdateSession()
        {
            if (CurrentUser != null)
            {
                SessionPersistence.SaveSession(CurrentUser);
            }
        }
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
                var directory = Path.GetDirectoryName(SessionFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var sessionData = new SessionFileData
                {
                    UserId = user.UserId,
                    Tokens = user.Tokens
                };

                var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SessionFilePath, json);
            }
            catch (Exception ex)
            {
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
                var sessionData = JsonSerializer.Deserialize<SessionFileData>(json);

                return new UserContext
                {
                    UserId = sessionData.UserId,
                    Tokens = sessionData.Tokens
                    // All other properties will be null/default and can be filled after backend calls
                };
            }
            catch (Exception ex)
            {
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
