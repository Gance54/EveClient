﻿using System;
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
        public string Email { get; set; }
        public string AuthToken { get; set; }
        public bool IsSubscribed { get; set; }
        public List<EveCharacterContext> Characters { get; set; } = new();
        public ProductionPlanner Planner { get; set; } = new();
        public TokenInfo Tokens { get; set; }

        public bool IsLoggedIn => Tokens != null;
        public bool IsActiveSubscriber => IsLoggedIn && IsSubscribed;
    }

    public static class SessionManager
    {
        private static UserContext _currentUser;
        
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
