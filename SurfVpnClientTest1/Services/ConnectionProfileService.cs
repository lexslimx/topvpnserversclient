using SurfVpnClientTest1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SurfVpnClientTest1.Services
{
    /* Search the current user's downloads folder for a connection profile if so get the profile name, and path and add them to the connection profile list */
    public class ConnectionProfileService : IConnectionProfileService
    {
        private readonly HttpClient _httpClient;

        public ConnectionProfileService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public List<ConnectionProfile> GetConnectionProfiles()
        {
            var connectionProfiles = new List<ConnectionProfile>();
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var vpnProfilePath = System.IO.Path.Combine(appDataPath, "TopVpnServers");
            if (System.IO.Directory.Exists(vpnProfilePath))
            {
                var files = System.IO.Directory.GetFiles(vpnProfilePath, "*.ovpn");
                foreach (var file in files)
                {
                    connectionProfiles.Add(new ConnectionProfile
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(file),
                        Path = file
                    });
                }
            }
            return connectionProfiles;
        }

        public async Task<string> DownloadProfileAsync(int subscriptionId, string ipaddress, string region)
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var vpnProfilePath = System.IO.Path.Combine(appDataPath, "TopVpnServers");
            if (!System.IO.Directory.Exists(vpnProfilePath))
            {
                System.IO.Directory.CreateDirectory(vpnProfilePath);
            }

            var url = $"https://localhost:7295/api/profile?subscriptionId={subscriptionId}&ipaddress={ipaddress}";
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var fileName = $"{region}.ovpn";
                var filePath = System.IO.Path.Combine(vpnProfilePath, fileName);

                using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                {
                    await response.Content.CopyToAsync(fs);
                }

                return filePath;
            }
            catch (HttpRequestException)
            {
                // Optionally log or handle errors
                throw new Exception("Failed to download the profile. Please check your connection or the server status.");                
            }
        }

        public void DeleteAllProfiles()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var vpnProfilePath = System.IO.Path.Combine(appDataPath, "TopVpnServers");
            if (System.IO.Directory.Exists(vpnProfilePath))
            {
                var files = System.IO.Directory.GetFiles(vpnProfilePath, "*.ovpn");
                foreach (var file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch
                    {
                        // Optionally log or handle errors for each file
                    }
                }
            }
        }
    }

    public interface IConnectionProfileService
    {
        public List<ConnectionProfile> GetConnectionProfiles();
        Task<string?> DownloadProfileAsync(int subscriptionId, string ipaddress, string region);
    }
}
