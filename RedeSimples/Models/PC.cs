using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class PC : NetworkDevice
    {
        public string OS { get; set; } = string.Empty;
        public string CPU { get; set; } = string.Empty;
        public int RAM { get; set; } = 0; // in GB
        public int Storage { get; set; } = 0; // in GB
        public PC(string name, string ip, string mac, Rectangle rectangle, string os, string cpu, int ram, int storage)
            : base(name, ip, mac, rectangle)
        {
            OS = os;
            CPU = cpu;
            RAM = ram;
            Storage = storage;
        }
        public PC() : base()
        {
        }
    }
}
