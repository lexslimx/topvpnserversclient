using CommunityToolkit.Mvvm.Input;
using SurfVpnClientTest1.Models;
using SurfVpnClientTest1.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SurfVpnClientTest1.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        string ovpnFilePath = @"C:\Users\alexk\Downloads\alexkm14@gmail.com-europe west.ovpn"; // Path to your .ovpn file
        string openVpnExePath = @"C:\Program Files\OpenVPN\bin\openvpn.exe"; // Adjust this path based on your OpenVPN installation

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
       PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private ConnectionProfileService connectionProfileService;

        public MainWindowViewModel()
        {
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            connectionProfileService = new ConnectionProfileService();
            IsVpnConnected();
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
                _connectionProfiles = connectionProfileService.GetConnectionProfiles();
                OnPropertyChanged(nameof(ConnectionProfiles));
            }
        }

        public string ConnectButtonText => IsConnected == true ? "Disconnect" : "Connect";

        // Intorduce property SelectedConnectionProfile
        private ConnectionProfile _selectedConnectionProfile;
        public ConnectionProfile SelectedConnectionProfile
        {
            get => _selectedConnectionProfile;
            set
            {
                _selectedConnectionProfile = value;
                OnPropertyChanged(nameof(SelectedConnectionProfile));
                // Update the ovpnFilePath based on the selected profile
                if (value != null)
                {
                    ovpnFilePath = value.Path;
                }
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
                if(value == _isConnected) return; // Avoid unnecessary updates
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(ConnectButtonText)); // Notify UI to update button text
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
                MessageBox.Show("Connected to VPN successfully.");
                ConnectionStatus = "Connected";
                IsConnected = true;
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
                            if (line.Contains("CONNECTED,SUCCESS")) { 
                                IsConnected = true;
                                return true;
                            }
                        }
                        writer.WriteLine("exit");
                    }
                }
            }
            catch
            {
                // Could not connect to management interface or parse state
                IsConnected = false;
                throw new InvalidOperationException("Could not determine VPN connection status. Is OpenVPN running?");
            }
            IsConnected = false;
            return false;
        }
    }
}
