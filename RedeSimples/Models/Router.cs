using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Router : NetworkDevice
    {
        public string model { get; set; } = string.Empty;
        public string brand { get; set; } = string.Empty;

        public Router(string name, string ip, string mac, Rectangle rectangle, string model, string brand)
            : base(name, ip, mac, rectangle)
        {
            this.model = model;
            this.brand = brand;
        }

        public Router()
        {
        }
    }
}
