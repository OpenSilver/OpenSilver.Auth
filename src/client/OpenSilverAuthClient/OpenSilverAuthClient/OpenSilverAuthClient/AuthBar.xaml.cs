using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OpenSilver;

namespace OpenSilverAuthClient
{
    public partial class AuthBar : UserControl
    {
        public static readonly DependencyProperty EmailProperty =
            DependencyProperty.Register(nameof(Email), typeof(string), typeof(AuthBar), new PropertyMetadata(""));

        public string Email
        {
            get => (string)GetValue(EmailProperty);
            set => SetValue(EmailProperty, value);
        }

        private readonly HttpClient _http = new HttpClient();

        public AuthBar()
        {
            InitializeComponent();
            Loaded += (s, e) => Initialize();
        }

        private void Initialize()
        {
            _ = RefreshAuthAsync();
            ExecuteJS("window.addEventListener('auth:changed', function(){ $0(); });",
                     (Action)(async () => await RefreshAuthAsync()));
        }

        private async Task RefreshAuthAsync()
        {
            var token = GetJSValue("window.Auth?.getToken?.() || ''");

            if (string.IsNullOrWhiteSpace(token))
            {
                SetUnauthenticated();
                return;
            }

            try
            {
                var email = await ValidateTokenAsync(token);
                SetAuthenticated(email);
            }
            catch
            {
                SetUnauthenticated();
            }
        }

        private async Task<string> ValidateTokenAsync(string token)
        {
            var apiBase = GetJSValue("window.AUTH_CFG?.apiBase || ''");
            if (string.IsNullOrWhiteSpace(apiBase))
                throw new InvalidOperationException("Missing AUTH_CFG.apiBase");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBase}/secure/ping");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("email").GetString() ?? "";
        }

        private void SetAuthenticated(string email)
        {
            Email = email;
            AuthedPanel.Visibility = Visibility.Visible;
            UnauthedPanel.Visibility = Visibility.Collapsed;
        }

        private void SetUnauthenticated()
        {
            Email = "";
            AuthedPanel.Visibility = Visibility.Collapsed;
            UnauthedPanel.Visibility = Visibility.Visible;
        }

        private string GetJSValue(string script) =>
            Interop.ExecuteJavaScript(script)?.ToString() ?? "";

        private void ExecuteJS(string script, params object[] args) =>
            Interop.ExecuteJavaScript(script, args);

        private void Login_Click(object sender, RoutedEventArgs e) =>
            ExecuteJS("window.Auth?.login?.()");

        private void Logout_Click(object sender, RoutedEventArgs e) =>
            ExecuteJS("window.Auth?.logout?.()");
    }
}