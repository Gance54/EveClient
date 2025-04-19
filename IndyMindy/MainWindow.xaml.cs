using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

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

            Console.WriteLine($"Selected Blueprint ID");
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

                var materials = Database.LoadMaterials(bp.BlueprintID);
                MaterialsList.ItemsSource = materials;

                BlueprintIcon.Source = new BitmapImage(new Uri(bp.ItemconUrl));

                var summaryList = materials.Select(m => new MaterialSummary
                {
                    MaterialName = m.MaterialName,
                    TypeID = m.MaterialTypeID,
                    Quantity = m.Quantity,
                    BuyPrice = 0,
                    SellPrice = 0
                }).ToList();

                SummaryList.ItemsSource = summaryList;
                TotalPriceText.Text = " - ";

                var estimated = await PriceProvider.GetEstimatedItemValueAsync(bp.ProductID);
                ItemPriceText.Text = $" {estimated:N2} ISK";
            }
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
            }

            SummaryList.Items.Refresh();

            FetchPricesButton.IsEnabled = true;
            FetchPricesButton.Content = "Fetch Prices";

            decimal totalBuy = summaries.Sum(x => x.BuyPrice * x.Quantity);
            decimal totalSell = summaries.Sum(x => x.SellPrice * x.Quantity);

            TotalPriceText.Text = $"Total: Buy - {totalBuy:N2} ISK  |  Sell - {totalSell:N2} ISK";
        }
    }
}

