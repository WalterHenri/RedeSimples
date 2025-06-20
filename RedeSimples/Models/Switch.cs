using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Switch : NetworkDevice
    {
        public int NumeroDePortas { get; set; } = 0;
        public int PortasOcupadas { get; set; } = 0;

        public Switch()
        {
        }
    }
}
