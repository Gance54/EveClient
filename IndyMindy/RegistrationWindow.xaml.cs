using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;

namespace EveIndyCalc
{
    public partial class RegistrationWindow : Window
    {
        private static readonly HttpClient http = new();

        public RegistrationWindow()
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

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirm = ConfirmBox.Password;

            // Clear previous status
            StatusText.Text = "";

            // Validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirm))
            {
                StatusText.Text = "All fields are required.";
                return;
            }

            if (password != confirm)
            {
                StatusText.Text = "Passwords do not match.";
                return;
            }

            if (password.Length < 6)
            {
                StatusText.Text = "Password must be at least 6 characters long.";
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
                // Send registration request to server
                var response = await http.PostAsync("http://localhost:5000/api/account/register", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Account created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close(); // Close the registration window
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    StatusText.Text = $"Registration failed: {error}";
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

        private void ConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ConfirmPlaceholder.Visibility = string.IsNullOrWhiteSpace(ConfirmBox.Password)
                ? Visibility.Visible
                : Visibility.Hidden;
        }
    }
}