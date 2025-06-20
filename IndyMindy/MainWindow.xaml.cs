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

        private List<Material> currentMaterials;
        private List<Blueprint> allBlueprints = new();
        private Blueprint? currentBlueprint;

        // for REST comm test
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl)
        };

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
            
            // Load existing session on startup
            LoadExistingSession();
            RedrawUI();
        }

        private void LoadExistingSession()
        {
            var savedUser = SessionPersistence.LoadSession();
            if (savedUser != null)
            {
                SessionManager.CurrentUser = savedUser;
                System.Diagnostics.Debug.WriteLine($"Loaded session for user: {savedUser.Email}");
            }
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

        private async void AddEveCharacterButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void RedrawUI()
        {
            var user = IndyMindy.SessionManager.CurrentUser;
            bool isLoggedIn = user != null;
            bool isSubscribed = isLoggedIn && user.IsSubscribed;

            OpenProductionPlannerButton.Visibility = (isLoggedIn && isSubscribed) ? Visibility.Visible : Visibility.Collapsed;
            LoginButton.Visibility = (!isLoggedIn) ? Visibility.Visible : Visibility.Collapsed;
            RegisterButton.Visibility = (!isLoggedIn) ? Visibility.Visible : Visibility.Collapsed;
            LogoutButton.Visibility = (isLoggedIn) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
            
            // Redraw UI after login attempt (whether successful or not)
            RedrawUI();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.ShowDialog();
        }

        private void OpenProductionPlannerButton_Click(object sender, RoutedEventArgs e)
        {
            var plannerWindow = new ProductionPlannerWindow();
            plannerWindow.Owner = this;
            plannerWindow.Show();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            SessionPersistence.ClearSession();
            SessionManager.CurrentUser = null;
            RedrawUI();
            MessageBox.Show("You have been logged out.");
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

