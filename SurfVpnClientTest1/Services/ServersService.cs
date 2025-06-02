using SurfVpnClientTest1.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SurfVpnClientTest1.Services
{
    internal class ServersService
    {
        private readonly HttpClient _httpClient;

        public ServersService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<ClientServer>> GetServersAsync(int subscriptionId)
        {
            var url = $"https://localhost:7295/api/server?subscriptionId={subscriptionId}";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var servers = JsonSerializer.Deserialize<List<ClientServer>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return servers;
            }
            catch (HttpRequestException)
            {
                // Optionally log or handle other errors
                throw new Exception("Get servers error");
                return new List<ClientServer>();
            }
        }
    }
}
