using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Cable
    {
        public int Index { get; set; } // Starts from 0 and increments for each cable
        public int StartX { get; set; } = 0;
        public int StartY { get; set; } = 0;
        public int EndX { get; set; } = 0;
        public int EndY { get; set; } = 0;
        public int Size { get; set; } = 0;
        public string Color { get; set; } = "Black";
    }
}
