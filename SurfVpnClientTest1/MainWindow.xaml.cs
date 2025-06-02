using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SurfVpnClientTest1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {            
            if (ConnectionStatus.Text == "Connected")
            {
                Disconnect();
            }
            else
            {
                Connect();
            }
        }



        private async void Disconnect() {
            // Kill the process
            Console.WriteLine("Searching for OpenVPN processes...");

            // Find all running processes with the name "openvpn"
            var openVpnProcesses = Process.GetProcessesByName("openvpn");

            if (!openVpnProcesses.Any())
            {
                Console.WriteLine("No active OpenVPN processes found.");
                return;
            }

            foreach (var process in openVpnProcesses)
            {
                try
                {
                    Console.WriteLine($"Terminating OpenVPN process with PID: {process.Id}");
                    process.Kill(); // Terminates the process
                    process.WaitForExit(); // Waits for the process to exit
                    Console.WriteLine($"Successfully terminated process with PID: {process.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to terminate OpenVPN process with PID: {process.Id}. Error: {ex.Message}");
                }
            }

            Console.WriteLine("OpenVPN disconnection complete.");

        }

        private async void Connect()
        {
            
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Allow dragging the window from anywhere
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}