using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class NetworkDevice
    {
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public string MAC { get; set; } = string.Empty;
        public Rectangle Rectangle { get; set; } = new Rectangle();

        public Room Room { get; set; } = new Room();

        public NetworkDevice(string name, string ip, string mac, Rectangle rectangle)
        {
            Name = name;
            IP = ip;
            MAC = mac;
            Rectangle = rectangle;
        }
        public NetworkDevice() { }
    }
}
