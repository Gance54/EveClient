using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using IndyMindy;

namespace EveIndyCalc
{
    public partial class LoginWindow : Window
    {
        private static readonly HttpClient http = new();

        public LoginWindow()
        {
            InitializeComponent();
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;

            // Clear previous status
            StatusText.Text = "";

            // Validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "Email and password are required.";
                return;
            }

            // Hash the password before sending
            string hashedPassword = HashPassword(password);

            // Create payload for the server
            var payload = new
            {
                email = email,
                password = hashedPassword
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                // Send login request to server
                var response = await http.PostAsync(ApiConfig.LoginEndpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (loginResponse != null)
                    {
                        // Create user context with real tokens
                        var userContext = new UserContext
                        {
                            Email = loginResponse.User.Email,
                            AuthToken = loginResponse.AccessToken,
                            IsSubscribed = loginResponse.User.IsSubscribed,
                            Characters = new List<EveCharacterContext>(),
                            Tokens = new TokenInfo
                            {
                                AccessToken = loginResponse.AccessToken,
                                RefreshToken = loginResponse.RefreshToken,
                                ExpiresIn = loginResponse.ExpiresIn,
                                TokenType = loginResponse.TokenType,
                                IssuedAt = loginResponse.IssuedAt
                            }
                        };

                        // Set as current user session
                        SessionManager.CurrentUser = userContext;

                        // Save session to persist across restarts
                        SessionPersistence.SaveSession(userContext);

                        MessageBox.Show($"Login successful! Welcome back, {userContext.Email}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Close();
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    StatusText.Text = $"Login failed: {error}";
                }
            }
            catch (HttpRequestException httpEx)
            {
                StatusText.Text = $"Connection error: {httpEx.Message}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Unexpected error: {ex.Message}";
            }
        }

        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrWhiteSpace(EmailBox.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrWhiteSpace(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            Close();
        }
    }

    // Response models for deserialization
    public class LoginResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "";
        public DateTime IssuedAt { get; set; }
        public UserResponse User { get; set; } = new();
    }

    public class UserResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public bool IsSubscribed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 