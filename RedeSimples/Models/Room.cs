using System.Collections.Generic;

namespace Models
{
    public class Room
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Rectangle Rectangle { get; set; } = new Rectangle();
        public List<NetworkDevice> NetworkDevices { get; set; } = new List<NetworkDevice>();
        public string BackgroundColor { get; set; } = "#80808080"; // Cinza semi-transparente padrão
        public string BorderColor { get; set; } = "#FF808080"; // Cinza escuro padrão
        public double BorderThickness { get; set; } = 2.0;
    }
}