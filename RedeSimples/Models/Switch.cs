using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Switch : NetworkDevice
    {
        public string Model { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public Switch(string name, string ip, string mac, Rectangle rectangle, string model, string brand)
            : base(name, ip, mac, rectangle)
        {
            Model = model;
            Brand = brand;
        }
        public Switch()
        {
        }
    }
}
