using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    [XmlRoot("NetworkLayout")]
    public class CanvasState
    {
        [XmlArray("Devices")]
        [XmlArrayItem("Device")]
        public List<NetworkDevice> Devices { get; set; } = new List<NetworkDevice>();

        [XmlArray("Rooms")]
        [XmlArrayItem("Room")]
        public List<Room> Rooms { get; set; } = new List<Room>();

        [XmlArray("Cables")]
        [XmlArrayItem("Cable")]
        public List<NetworkCable> Cables { get; set; } = new List<NetworkCable>();
    }
}