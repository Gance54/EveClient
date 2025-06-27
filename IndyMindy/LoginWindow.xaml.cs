using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;
using IndyMindy;
using IndyMindy.Services;

namespace EveIndyCalc
{
    public partial class LoginWindow : Window
    {
        private readonly IAccountService _accountService;

        public LoginWindow()
        {
            InitializeComponent();
            var httpService = new HttpService();
            _accountService = new AccountService(httpService);
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

            try
            {
                // Send login request using the service
                var request = new LoginRequest
                {
                    Email = email,
                    Password = hashedPassword
                };

                var loginResponse = await _accountService.LoginAsync(request);

                if (loginResponse != null)
                {
                    // Create user context with real tokens
                    var userContext = new UserContext
                    {
                        UserId = loginResponse.UserId,
                        Email = loginResponse.Email,
                        AuthToken = loginResponse.AccessToken,
                        IsSubscribed = false, // TODO: Get from backend
                        Characters = new List<EveCharacterContext>(),
                        Tokens = new TokenInfo
                        {
                            AccessToken = loginResponse.AccessToken,
                            RefreshToken = loginResponse.RefreshToken,
                            TokenType = loginResponse.TokenType,
                        }
                    };

                    // Set as current user session
                    SessionManager.CurrentUser = userContext;

                    MessageBox.Show($"Login successful! Welcome back, {userContext.Email}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
            }
            catch (HttpServiceException ex)
            {
                StatusText.Text = $"Login failed: {ex.Message}";
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
}