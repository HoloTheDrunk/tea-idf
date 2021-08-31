using System.Collections.Generic;

namespace World.Parsing
{
    public struct OsmOutput
    {
        public double Version;
        public string Generator;
        public Osm3S Osm3S;
        public List<Element> Elements;
    }
}