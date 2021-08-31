using System.Collections.Generic;

namespace World.Parsing
{ 
    public struct Element
    {
        public string Type;
        public ulong ID;
        public double Lat;
        public double Lon;
        public List<ulong> Nodes;
    }
}