using SurfVpnClientTest1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurfVpnClientTest1.Services
{
    /* Search the current user's downloads folder for a connection profile if so get the profile name, and path and add them to the connection profile list */
    public class ConnectionProfileService : IConnectionProfileService
    {
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
    }

    public interface IConnectionProfileService
    {
        public List<ConnectionProfile> GetConnectionProfiles();
    }
}
