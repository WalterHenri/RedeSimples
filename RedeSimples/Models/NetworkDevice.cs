using System.Xml.Serialization;

namespace Models
{
    [XmlInclude(typeof(PC))]
    [XmlInclude(typeof(Router))]
    [XmlInclude(typeof(Switch))]
    [XmlInclude(typeof(Printer))]
    public class NetworkDevice
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public string MAC { get; set; } = string.Empty;
        public string model { get; set; } = string.Empty;
        public string brand { get; set; } = string.Empty;
        public Rectangle Rectangle { get; set; } = new Rectangle();

        [XmlIgnore] 
        public Room? Room { get; set; }

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