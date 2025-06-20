using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    public class NetworkCable
    {
        public int Id { get; set; }

        [XmlIgnore] // A lista de objetos não será serializada diretamente
        public List<NetworkDevice> ConnectedDevices { get; set; } = new List<NetworkDevice>();

        // Usaremos uma lista de IDs para a serialização para evitar referências circulares
        [XmlArray("ConnectedDeviceIDs")]
        [XmlArrayItem("ID")]
        public List<int> ConnectedDeviceIds { get; set; } = new List<int>();

        public List<Cable> Cables { get; set; } = new List<Cable>();
        public string Description { get; set; } = "Ethernet Cable";
    }
}