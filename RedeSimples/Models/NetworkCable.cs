using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class NetworkCable
    {
        public int Id { get; set; }

        public List<NetworkDevice> ConnectedDevices { get; set; } = new List<NetworkDevice>();

        public List<Cable> Cables { get; set; } = new List<Cable>();

        public string Description { get; set; } = "Ethernet Cable"; 
    }
}
