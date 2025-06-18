using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Newtonsoft.Json;
using IndyMindy;
using System.Windows.Media.TextFormatting;

namespace EveIndyCalc
{
    public partial class MainWindow : Window
    {
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCLIENT = 1;
        private const int WM_NCHITTEST = 0x0084;

        private const string clientId = "cccbeb7e7fe34dd2a4ff5587d609b29c";
        private const string secretKey = "fsKrbz98kls015vReZ9rLkUG8rFjkN9iZTmgYh4b";
        private const string callbackUrl = "http://localhost:5000/callback";
        private const string loginUrl = "https://login.eveonline.com/v2/oauth/authorize/";
        private const string tokenUrl = "https://login.eveonline.com/v2/oauth/token";
        private const string scopes = "publicData esi-skills.read_skills.v1 esi-skills.read_skillqueue.v1 esi-characters.read_contacts.v1 esi-assets.read_assets.v1 esi-characters.write_contacts.v1 esi-characters.read_loyalty.v1 esi-characters.read_chat_channels.v1 esi-characters.read_medals.v1 esi-characters.read_standings.v1 esi-characters.read_agents_research.v1 esi-industry.read_character_jobs.v1 esi-characters.read_blueprints.v1 esi-characters.read_corporation_roles.v1 esi-characters.read_fatigue.v1 esi-characters.read_notifications.v1 esi-assets.read_corporation_assets.v1 esi-industry.read_corporation_jobs.v1 esi-industry.read_character_mining.v1 esi-industry.read_corporation_mining.v1 esi-characters.read_titles.v1 esi-characters.read_fw_stats.v1";

        private List<Material> currentMaterials;
        private List<Blueprint> allBlueprints = new();
        private Blueprint? currentBlueprint;

        public MainWindow()
        {
            InitializeComponent();
            allBlueprints = Database.LoadBlueprints();
            BlueprintList.ItemsSource = allBlueprints;
            SourceInitialized += (s, e) =>
            {
                IntPtr handle = (new WindowInteropHelper(this)).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text.Trim().ToLower();

            // Toggle placeholder visibility
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(query)
                ? Visibility.Visible
                : Visibility.Collapsed;

            // Filter blueprints
            var filtered = allBlueprints
                .Where(bp => bp.Name != null && bp.Name.ToLower().Contains(query))
                .ToList();

            BlueprintList.ItemsSource = filtered;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Collapsed
                : Visibility.Hidden;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrWhiteSpace(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private async void BlueprintList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BlueprintList.SelectedItem is Blueprint bp)
            {
                NameText.Text = bp.Name;

                TimeSpan time = TimeSpan.FromSeconds(bp.ProductionTime);
                List<string> parts = new();

                if (time.Days > 0)
                    parts.Add($"{time.Days} Day{(time.Days > 1 ? "s" : "")}");
                if (time.Hours > 0)
                    parts.Add($"{time.Hours} Hour{(time.Hours > 1 ? "s" : "")}");
                if (time.Minutes > 0)
                    parts.Add($"{time.Minutes} Minute{(time.Minutes > 1 ? "s" : "")}");
                if (time.Seconds > 0 || parts.Count == 0)
                    parts.Add($"{time.Seconds} Second{(time.Seconds > 1 ? "s" : "")}");

                ProdTimeText.Text = string.Join(" ", parts);
                OutputQuantityText.Text = bp.ProductQuantity.ToString();
                ItemTotalValueText.Text = string.Empty;

                if (bp.ProductQuantity > 1)
                {
                    OutputQuantityText.Visibility = Visibility.Visible;
                    OutputQuantityLabel.Visibility = Visibility.Visible;
                    ItemTotalValueText.Visibility = Visibility.Visible;
                    ItemTotalValueLabel.Visibility = Visibility.Visible;

                    var estimatedSingle = await PriceProvider.GetEstimatedItemValueAsync(bp.ProductID);
                    ItemPriceText.Text = $" {estimatedSingle:N2} ISK";
                    decimal total = estimatedSingle * bp.ProductQuantity;
                    ItemTotalValueText.Text = $" {total:N2} ISK";
                }
                else
                {
                    OutputQuantityText.Visibility = Visibility.Collapsed;
                    OutputQuantityLabel.Visibility = Visibility.Collapsed;
                    ItemTotalValueText.Visibility = Visibility.Collapsed;
                    ItemTotalValueLabel.Visibility = Visibility.Collapsed;

                    var estimatedSingle = await PriceProvider.GetEstimatedItemValueAsync(bp.ProductID);
                    ItemPriceText.Text = $" {estimatedSingle:N2} ISK";
                }

                // ✅ Build a deep-loaded version of the blueprint
                currentBlueprint = new Blueprint
                {
                    BlueprintID = bp.BlueprintID,
                    ProductID = bp.ProductID,
                    Name = bp.Name,
                    ProductionTime = bp.ProductionTime,
                    Materials = Database.LoadMaterials(bp.BlueprintID)
                };

                // ✅ Update icon and material list
                BlueprintIcon.Source = new BitmapImage(new Uri(currentBlueprint.ItemconUrl));
                var flatList = FlattenMaterials(currentBlueprint);
                MaterialsList.ItemsSource = flatList;
                UpdateSummary(flatList);

                // ✅ Load estimated item price
                TotalPriceText.Text = " - ";
            }
        }

        private void BuildInsteadOfBuy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is BlueprintMaterial changedMaterial)
            {           
                // Re-flatten the currentBlueprint after the toggle
                if (currentBlueprint != null)
                {
                    var flat = FlattenMaterials(currentBlueprint);
                    MaterialsList.ItemsSource = flat;
                    UpdateSummary(flat);
                }
            }
        }

        private void UpdateSummary(List<BlueprintMaterial> flatMaterials)
        {
            var grouped = flatMaterials
                .Where(m => !m.BuildInsteadOfBuy || m.NestedBlueprint == null)
                .GroupBy(m => m.MaterialTypeID)
                .Select(g => new MaterialSummary
                {
                    MaterialName = g.First().MaterialName,
                    TypeID = g.First().MaterialTypeID,
                    Quantity = g.Sum(x => x.DisplayQuantity),
                    BuyPrice = 0,
                    SellPrice = 0
                })
                .ToList();

            SummaryList.ItemsSource = grouped;
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                Point position = PointFromScreen(new Point((lParam.ToInt32() & 0xFFFF), (lParam.ToInt32() >> 16)));
                double edgeSize = 8;

                if (position.X <= edgeSize)
                {
                    if (position.Y <= edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTTOPLEFT;
                    }
                    else if (position.Y >= ActualHeight - edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOMLEFT;
                    }

                    handled = true;
                    return (IntPtr)HTLEFT;
                }
                else if (position.X >= ActualWidth - edgeSize)
                {
                    if (position.Y <= edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTTOPRIGHT;
                    }
                    else if (position.Y >= ActualHeight - edgeSize)
                    {
                        handled = true;
                        return (IntPtr)HTBOTTOMRIGHT;
                    }

                    handled = true;
                    return (IntPtr)HTRIGHT;
                }
                else if (position.Y <= edgeSize)
                {
                    handled = true;
                    return (IntPtr)HTTOP;
                }
                else if (position.Y >= ActualHeight - edgeSize)
                {
                    handled = true;
                    return (IntPtr)HTBOTTOM;
                }
            }

            return IntPtr.Zero;
        }

        private async void FetchPrices_Click(object sender, RoutedEventArgs e)
        {
            if (SummaryList.ItemsSource is not IEnumerable<MaterialSummary> summaries)
                return;

            FetchPricesButton.IsEnabled = false;
            FetchPricesButton.Content = "Fetching...";

            foreach (var item in summaries)
            {
                var (buy, sell) = await PriceProvider.GetPricesAsync(item.TypeID);
                item.BuyPrice = buy;
                item.SellPrice = sell;

                System.Diagnostics.Debug.WriteLine($"Fetching prices: {item.MaterialName} Quant={item.Quantity}, sell = {item.SellPrice}, buy = {item.BuyPrice}");
            }

            SummaryList.Items.Refresh();

            FetchPricesButton.IsEnabled = true;
            FetchPricesButton.Content = "Fetch Prices";

            decimal totalBuy = summaries.Sum(x => x.BuyPrice * x.Quantity);
            decimal totalSell = summaries.Sum(x => x.SellPrice * x.Quantity);

            TotalPriceText.Text = $"Total: Buy - {totalBuy:N2} ISK  |  Sell - {totalSell:N2} ISK";
        }



        public static List<BlueprintMaterial> FlattenMaterials(Blueprint blueprint, double multiplier = 1, int depth = 0)
        {
            var flat = new List<BlueprintMaterial>();

            foreach (var mat in blueprint.Materials)
            {
                mat.Depth = depth;

                // This is the actual quantity we need of this material
                mat.DisplayQuantity = (int)Math.Ceiling(mat.Quantity * multiplier);

                flat.Add(mat);

                if (mat.BuildInsteadOfBuy && mat.NestedBlueprint != null)
                {
                    double producedPerRun = Math.Max(mat.NestedBlueprint.ProductQuantity, 1);
                    double neededItems = mat.Quantity * multiplier;
                    double runsNeeded = neededItems / producedPerRun;

                    flat.AddRange(
                        FlattenMaterials(mat.NestedBlueprint, runsNeeded, depth + 1)
                    );
                }
            }

            return flat;
        }

        private async Task<(long characterId, string characterName)> GetCharacterInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://esi.evetech.net/verify");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to verify token: " + await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            long characterId = data.CharacterID;
            string characterName = data.CharacterName;

            return (characterId, characterName);
        }

        private async void AddEveCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            string state = Guid.NewGuid().ToString(); // Prevent CSRF
            string authUrl = $"{loginUrl}?response_type=code&redirect_uri={Uri.EscapeDataString(callbackUrl)}&client_id={clientId}&scope={Uri.EscapeDataString(scopes)}&state={state}";

            // Launch browser
            Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

            // Start local HTTP listener
            HttpListener listener = new();
            listener.Prefixes.Add(callbackUrl + "/");
            listener.Start();

            var context = await listener.GetContextAsync();

            // ✅ Respond to browser so user sees a success page
            var browserResponse = context.Response;
            string responseHtml = "<html><body><h2>Authentication successful! You may now return to the app.</h2></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
            browserResponse.ContentLength64 = buffer.Length;
            browserResponse.OutputStream.Write(buffer, 0, buffer.Length);
            browserResponse.OutputStream.Close();

            string code = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("code");
            string returnedState = HttpUtility.ParseQueryString(context.Request.Url.Query).Get("state");

            listener.Stop();

            if (returnedState != state)
            {
                MessageBox.Show("State mismatch. Possible CSRF.");
                return;
            }

            // Exchange code for token
            using var client = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secretKey}"))
            );
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
            });

            var response = await client.SendAsync(tokenRequest);
            string json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Token exchange failed:\n" + json);
                return;
            }

            dynamic tokenData = JsonConvert.DeserializeObject(json);
            string accessToken = tokenData.access_token;
            string refreshToken = tokenData.refresh_token;
            int expiresIn = tokenData.expires_in;

            var (characterId, characterName) = await GetCharacterInfoAsync(accessToken);

            EveCharacterContext character = new EveCharacterContext
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn),
                CharacterID = characterId,
                CharacterName = characterName,
                Scopes = scopes.Split(' ')
            };

            SessionManager.CurrentUser.Characters.Add(character);

            MessageBox.Show("Login successful!\n" + SessionManager.CurrentUser?.Characters?[0]?.CharacterName);
        }

        private /* async */ void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            // Simulated backend login result
            var dummyUser = new UserContext
            {
                Email = "dummy@example.com",
                AuthToken = "dummy_token_123",
                IsSubscribed = true,
                Characters = new List<EveCharacterContext>() // No characters yet
            };

            // Set as current user session
            SessionManager.CurrentUser = dummyUser;

            LoginButton.Visibility = Visibility.Collapsed;
            RegisterButton.Visibility = Visibility.Collapsed;
            AddEveCharacterButton.Visibility = Visibility.Visible;


            MessageBox.Show($"Logged in as {dummyUser.Email} (no characters linked yet).");
        }

        private /* async */ void RegisterButton_Click(object sender, RoutedEventArgs e)
        {

            MessageBox.Show("Register placeholder!\n" + SessionManager.CurrentUser?.Characters?[0]?.CharacterName);
        }
    }

    public class DepthToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int depth)
            {
                return new Thickness(depth * 15, 0, 0, 0); // 15px per level
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

