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

        Process openVpnProcess;

        public MainWindowViewModel()
        {
            ConnectCommand = new RelayCommand(Connect, CanConnect);
            connectionProfileService = new ConnectionProfileService();
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
            // CHeck if the selected connection profile is null or empty
            if (SelectedConnectionProfile == null || string.IsNullOrEmpty(SelectedConnectionProfile.Path))
            {
                LogsTextBlock = "Please select a valid connection profile.";
                return;
            }
            
            LogsTextBlock = "Starting OpenVPN connection...";

            // Start OpenVPN process
            openVpnProcess = new Process
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
                _isConnected = false;
            }
            else
            {
                MessageBox.Show("Connected to VPN successfully.");
                ConnectionStatus = "Connected";
                _isConnected = true;
            }
        }

        public void Disconnect()
        {
            if (openVpnProcess != null && !openVpnProcess.HasExited)
            {
                try
                {
                    openVpnProcess.Kill(true); // Kill the process and any child processes
                    openVpnProcess.WaitForExit();
                    LogsTextBlock = "Disconnected from VPN.";
                    ConnectionStatus = "Disconnected";
                    _isConnected = false;
                }
                catch (Exception ex)
                {
                    LogsTextBlock = $"Error disconnecting: {ex.Message}";
                }
            }
            else
            {
                LogsTextBlock = "No active VPN connection to disconnect.";
            }
        }

        private bool CanConnect()
        {
            // Command can only execute if not already connected
            return !_isConnected;
        }
    }
}
