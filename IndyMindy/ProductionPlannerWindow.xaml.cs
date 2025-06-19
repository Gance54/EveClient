using IndyMindy;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Interop;
using static System.Formats.Asn1.AsnWriter;

namespace EveIndyCalc
{
    public partial class ProductionPlannerWindow : Window
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

        public ProductionPlannerWindow()
        {
            InitializeComponent();
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

        private async Task<dynamic> GetCharacterInfoAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://esi.evetech.net/verify");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Failed to verify token: " + await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(json);

            return data;
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

            // ? Respond to browser so user sees a success page
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

            dynamic verifyData = await GetCharacterInfoAsync(accessToken);

            EveCharacterContext character = new EveCharacterContext
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn),
                CharacterID = verifyData.CharacterID,
                CharacterName = verifyData.CharacterName,
                Scopes = scopes.Split(' ')
            };

            SessionManager.CurrentUser.Characters.Add(character);

            MessageBox.Show("Login successful!\n" + SessionManager.CurrentUser?.Characters?[0]?.CharacterName);
        }
    }
} 