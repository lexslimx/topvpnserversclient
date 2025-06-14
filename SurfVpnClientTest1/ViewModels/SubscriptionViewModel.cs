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
        public SubscriptionViewModel() 
        {
            connectionProfileService = new ConnectionProfileService();
            SubscriptionId = GetSubscriptionId();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public RelayCommand UpdateSubscriptionCommand => new RelayCommand(() => SaveSubscriptionIdAsync().ConfigureAwait(false));
        private async Task SaveSubscriptionIdAsync()
        {

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string settingsPath = Path.Combine(appDataPath, "TopVpnServers", "topvpnserversettings.json");

            // Ensure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            var settings = new { subscriptionId = SubscriptionId };
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settingsPath, json);

            // On subscriptionUpdate, Get profiles
            connectionProfileService.DeleteAllProfiles();
            await GetProfilesFromApiAsync();
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
    }
}
