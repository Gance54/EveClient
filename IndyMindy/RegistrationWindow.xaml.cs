using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace EveIndyCalc
{
    public partial class RegistrationWindow : Window
    {
        private static readonly HttpClient http = new();

        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailBox.Text.Trim();
            string password = PasswordBox.Password;
            string confirm = ConfirmBox.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                StatusText.Text = "All fields required.";
                return;
            }

            if (password != confirm)
            {
                StatusText.Text = "Passwords do not match.";
                return;
            }

            var payload = new
            {
                email = email,
                password = password
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var response = await http.PostAsync("http://localhost:5000/api/account/register", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Account created successfully!", "Success");
                    Close(); // or auto-login
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    StatusText.Text = $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmailPlaceholder.Visibility = string.IsNullOrWhiteSpace(EmailBox.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }
    }
}