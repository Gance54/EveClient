using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;
using IndyMindy;
using IndyMindy.Services;

namespace EveIndyCalc
{
    public partial class RegistrationWindow : Window
    {
        private readonly IAccountService _accountService;

        public RegistrationWindow()
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

            try
            {
                // Send registration request using the service
                var request = new RegisterRequest
                {
                    Email = email,
                    Password = hashedPassword,
                    ConfirmPassword = hashedPassword
                };

                var response = await _accountService.RegisterAsync(request);

                if (response != null)
                {
                    MessageBox.Show("Account created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close(); // Close the registration window
                }
            }
            catch (HttpServiceException ex)
            {
                StatusText.Text = $"Registration failed: {ex.Message}";
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