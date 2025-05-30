using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SurfVpnClientTest1.Models;
using SurfVpnClientTest1.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SurfVpnClientTest1.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {        
        string openVpnExePath = @"C:\Program Files\OpenVPN\bin\openvpn.exe";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
       PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private ConnectionProfileService connectionProfileService;

        public MainWindowViewModel()
        {
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            connectionProfileService = new ConnectionProfileService();
            var connected = IsVpnConnected();
            ConnectButtonText = connected ? "Disconnect" : "Connect";
        }

        public ICommand ConnectCommand { get; private set; }

        private List<ConnectionProfile> _connectionProfiles;
        public List<ConnectionProfile> ConnectionProfiles
        {
            get
            {
                _connectionProfiles = connectionProfileService.GetConnectionProfiles();
                return _connectionProfiles;
            }
            set
            {
                _connectionProfiles = value;
                OnPropertyChanged(nameof(ConnectionProfiles));
            }
        }

        public ICommand DeleteProfileCommand => new RelayCommand(DeleteProfile);

        private void DeleteProfile()
        {
            var profile = SelectedConnectionProfile;
            if (profile == null || string.IsNullOrEmpty(profile.Path) || !File.Exists(profile.Path))
            {
                Console.WriteLine("Invalid profile selected or file does not exist.");
                ConnectionProfiles = connectionProfileService.GetConnectionProfiles(); // Refresh the list
                return;
            }
            try
            {
                File.Delete(profile.Path);
                ConnectionProfiles.Remove(profile);
                SelectedConnectionProfile = null; // Clear selection after deletion
                Console.WriteLine("Profile deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting profile: {ex.Message}");
                throw new Exception($"Error deleting profile: {ex.Message}");
            }
        }

        private string _connectButtonText;
        public string ConnectButtonText
        {
            get => _connectButtonText;
            set
            {
                if (_connectButtonText != value)
                {
                    _connectButtonText = value;
                    OnPropertyChanged(nameof(ConnectButtonText));
                }
            }
        }

        // Intorduce property SelectedConnectionProfile
        private ConnectionProfile _selectedConnectionProfile;
        public ConnectionProfile SelectedConnectionProfile
        {
            get => _selectedConnectionProfile;
            set
            {
                _selectedConnectionProfile = value;
                OnPropertyChanged(nameof(SelectedConnectionProfile));
            }
        }

        private string _connectionStatus = "Disconnected";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        private string _logsTextBlock = "";
        private bool _isConnected = false;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {                
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                ConnectButtonText = _isConnected ? "Disconnect" : "Connect";
            }
        }

        public string LogsTextBlock
        {
            get => _logsTextBlock;
            set
            {
                _logsTextBlock = value;
                OnPropertyChanged(nameof(LogsTextBlock));
            }
        }

        public async void Connect()
        {
            if (IsVpnConnected())
            {
                Disconnect();
                return;
            }

            // CHeck if the selected connection profile is null or empty
            if (SelectedConnectionProfile == null || string.IsNullOrEmpty(SelectedConnectionProfile.Path))
            {
                LogsTextBlock = "Please select a valid connection profile.";
                return;
            }

            LogsTextBlock = "Starting OpenVPN connection...";

            // Start OpenVPN process
            Process openVpnProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = openVpnExePath,
                    Arguments = $"--config \"{SelectedConnectionProfile.Path}\" --management 127.0.0.1 7505",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            // Redirect output and error streams
            openVpnProcess.OutputDataReceived += (sender, e) => LogsTextBlock = e.Data;
            openVpnProcess.ErrorDataReceived += (sender, e) => LogsTextBlock = ("ERROR: " + e.Data);


            openVpnProcess.Start();

            // Start reading outputs
            openVpnProcess.BeginOutputReadLine();
            openVpnProcess.BeginErrorReadLine();

            ConnectionStatus = "Connected";
            IsConnected = true;

            // Wait for the process to exit
            await openVpnProcess.WaitForExitAsync();

            LogsTextBlock = $"OpenVPN process exited with code {openVpnProcess.ExitCode}.";

            // Check if the process exited successfully
            if (openVpnProcess.ExitCode != 0)
            {
                LogsTextBlock = "Failed to connect to the VPN. Check the OpenVPN logs for more details.";
                ConnectionStatus = "Disconnected";
                IsConnected = false;
            }
            else
            {
                Console.WriteLine("Connected to VPN successfully.");
                ConnectionStatus = "Disconnected";
                IsConnected = false;
            }
        }

        public void Disconnect()
        {
            // Get the running openvpn process
            var openVpnProcess = Process.GetProcessesByName("openvpn").FirstOrDefault();

            if (openVpnProcess == null)
            {
                Console.WriteLine("No OpenVPN processes are running.");
                IsConnected = false;
                return;
            }

            try
            {
                // Try to send 'signal SIGTERM' to the OpenVPN management interface
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    client.Connect("127.0.0.1", 7505);
                    using (var stream = client.GetStream())
                    using (var writer = new StreamWriter(stream) { AutoFlush = true })
                    using (var reader = new StreamReader(stream))
                    {
                        // Read initial greeting
                        string line;
                        while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                        {
                            if (line.Contains("ENTER PASSWORD") || line.Contains(">")) break;
                        }
                        // Send SIGTERM command
                        writer.WriteLine("signal SIGTERM");
                        // Optionally, read response
                        while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                        {
                            if (line.Contains(">")) break;
                        }
                        writer.WriteLine("exit");
                    }
                }

                openVpnProcess.WaitForExit(5000); // Wait up to 5 seconds for process to exit

                if (!openVpnProcess.HasExited)
                {
                    openVpnProcess.Kill(true); // Fallback: force kill if still running
                    openVpnProcess.WaitForExit();
                    IsConnected = false;
                }

                LogsTextBlock = "Disconnected from VPN.";
                ConnectionStatus = "Disconnected";
                IsConnected = false;
            }
            catch (Exception ex)
            {
                LogsTextBlock = $"Error disconnecting: {ex.Message}";
                IsConnected = false;
                ConnectionStatus = "Disconnected";
                throw new Exception(ex.Message);
            }
        }

        private bool CanConnect()
        {
            // Command can only execute if not already connected
            return true;
        }

        private bool IsVpnConnected()
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    client.Connect("127.0.0.1", 7505);
                    using (var stream = client.GetStream())
                    using (var writer = new StreamWriter(stream) { AutoFlush = true })
                    using (var reader = new StreamReader(stream))
                    {
                        // Read initial greeting
                        string line;
                        while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                        {
                            if (line.Contains(">")) break;
                        }
                        // Send 'state' command to management interface
                        writer.WriteLine("state");
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains(">")) break;
                            // Look for a line like: >STATE,...,CONNECTED,SUCCESS,...
                            if (line.Contains("CONNECTED,SUCCESS"))
                            {                                
                                return true;
                            }
                        }
                        writer.WriteLine("exit");
                    }
                }
            }
            catch
            {                                
                Console.WriteLine("Could not determine VPN connection status. Is OpenVPN running?");
            }            
            return false;
        }

        private async Task<List<string>> GetBandwidthUsageAsync()
        {

            const string host = "127.0.0.1";
            const int port = 7505;
            var output = new List<string>();

            try
            {
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(host, port);

                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream);
                using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                // Read welcome banner
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.Contains("OpenVPN")) break;
                }

                // Send bytecount command
                await writer.WriteLineAsync("bytecount");

                // Read response
                string response = await reader.ReadLineAsync();

                if (response != null && response.StartsWith("SUCCESS:"))
                {
                    Console.WriteLine(response);

                    // Parse values
                    var parts = response.Split(' ');
                    if (parts.Length >= 6 &&
                        long.TryParse(parts[2], out long rxBytes) &&
                        long.TryParse(parts[5], out long txBytes))
                    {
                        Console.WriteLine($"RX: {rxBytes} bytes");
                        Console.WriteLine($"TX: {txBytes} bytes");

                        output.Add($"RX: {rxBytes} bytes");
                        output.Add($"TX: {txBytes} bytes");
                    }
                }

                // Exit
                await writer.WriteLineAsync("quit");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return output;
        }


        public ICommand ImportProfileCommand => new RelayCommand(ImportOvpnProfile);
        public void ImportOvpnProfile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "OpenVPN Config (*.ovpn)|*.ovpn",
                Title = "Select OpenVPN Profile"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string vpnProfilePath = Path.Combine(appDataPath, "TopVpnServers");

                try
                {
                    // Ensure the folder exists
                    Directory.CreateDirectory(vpnProfilePath);

                    string destFile = Path.Combine(vpnProfilePath, Path.GetFileName(selectedFile));
                    File.Copy(selectedFile, destFile, overwrite: true);

                    LogsTextBlock = $"Profile imported: {destFile}";

                    ConnectionProfiles = connectionProfileService.GetConnectionProfiles();
                    SelectedConnectionProfile = ConnectionProfiles.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    LogsTextBlock = $"Error importing profile: {ex.Message}";
                }
            }
        }
    }
    
    
}
