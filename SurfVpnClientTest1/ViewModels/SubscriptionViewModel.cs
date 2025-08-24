using CommunityToolkit.Mvvm.Input;
using SurfVpnClientTest1.Interfaces;
using SurfVpnClientTest1.Models;
using SurfVpnClientTest1.Services;
using SurfVpnClientTest1.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SurfVpnClientTest1.ViewModels
{
    public class SubscriptionViewModel : INotifyPropertyChanged
    {
        private ConnectionProfileService connectionProfileService;
        private readonly PkceAuthService _pkceAuthService = new PkceAuthService();
        public SubscriptionViewModel() 
        {
            connectionProfileService = new ConnectionProfileService();
            SubscriptionId = GetSubscriptionId();
            IsBusy = false;
            LoginPkceCommand = new AsyncRelayCommand(LoginWithPkce);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public RelayCommand UpdateSubscriptionCommand => new RelayCommand(() => SaveSubscriptionIdAsync().ConfigureAwait(false));
        public IRelayCommand LoginPkceCommand { get; }
        private async Task SaveSubscriptionIdAsync()
        {
            IsBusy = true;
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsPath = Path.Combine(appDataPath, "TopVpnServers", "topvpnserversettings.json");

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            var settings = new { subscriptionId = SubscriptionId };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);

            // On subscriptionUpdate, Get profiles
            connectionProfileService.DeleteAllProfiles();
            await GetProfilesFromApiAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    IsBusy = false;
                    StatusText = "Error updating subscription: " + t.Exception?.GetBaseException().Message;
                }
                else
                {
                   IsBusy = false;
                   StatusText = "Subscription updated successfully.";                    
                }
            });
        }

        private string _subscriptionId;
        public string SubscriptionId
        {
            get => _subscriptionId;
            set
            {
                if (_subscriptionId != value)
                {
                    _subscriptionId = value;
                    OnPropertyChanged(nameof(SubscriptionId));
                }
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }
        //StatusText
        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private async Task GetProfilesFromApiAsync()
        {
            ServersService serversService = new ServersService();
            string subscriptionId = GetSubscriptionId();
            List<ClientServer> clientServers = await serversService.GetServersAsync(int.Parse(subscriptionId));

            if (clientServers == null) return;

            // For each IP address, download the profile and add it to the connection profiles list
            foreach (var clientServer in clientServers)
            {
                try
                {
                    var profilePath = await connectionProfileService.DownloadProfileAsync(208, clientServer.IpAddress, clientServer.Region);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error downloading profile for {clientServer}: {ex.Message}");
                }
            }
        }
        public string GetSubscriptionId()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsPath = Path.Combine(appDataPath, "TopVpnServers", "topvpnserversettings.json");

            if (!File.Exists(settingsPath))
                return string.Empty;

            try
            {
                string json = File.ReadAllText(settingsPath);
                using var doc = JsonDocument.Parse(json);
                // Convert to SettingsFileModel
                var settingsFile = System.Text.Json.JsonSerializer.Deserialize<SettingsFileModel>(json);
                return settingsFile?.subscriptionId.ToString() ?? "0";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading subscription ID from settings file.");
                throw;
            }
            return string.Empty;
        }

        private async Task LoginWithPkce()
        {
            string redirectUri = "http://localhost:62348/callback";
            var listenTask = _pkceAuthService.ListenForRedirectAsync(redirectUri);

            string authUrl = _pkceAuthService.GetAuthorizationUrl(redirectUri);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });
            // After user logs in, handle the redirect and token exchange as needed
            
            string code = await listenTask;
            string tokenResponse = await _pkceAuthService.ExchangeCodeForTokenAsync(code, redirectUri);
            Console.WriteLine(tokenResponse);
            // Decode the token response to get claims
            var tokenData = JsonSerializer.Deserialize<Dictionary<string, object>>(tokenResponse);
            // Decode the id_token to get user info
            if (tokenData != null && tokenData.TryGetValue("id_token", out var idTokenObj))
            {
                string idToken = idTokenObj.ToString();
                var claims = ParseJwt(idToken);
                if (claims != null && claims.TryGetValue("sub", out var subObj))
                {
                    string sub = subObj.ToString();
                    // Set SubscriptionId to sub claim value
                    SubscriptionId = sub;
                    await SaveSubscriptionIdAsync();
                }
            }
        }

        private Dictionary<string, object> ParseJwt(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                return null;

            // JWT format: header.payload.signature
            var parts = idToken.Split('.');
            if (parts.Length < 2)
                return null;

            string payload = parts[1];

            // Pad the string for base64 decoding
            int mod4 = payload.Length % 4;
            if (mod4 > 0)
                payload += new string('=', 4 - mod4);

            try
            {
                var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                var json = Encoding.UTF8.GetString(bytes);
                var claims = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return claims;
            }
            catch
            {
                return null;
            }
        }
    }
}
