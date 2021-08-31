namespace World.Parsing{ 

    public struct SchoolInfo
    {
        public int ID;
        public int Ranking;
        public string Name;
        public string Category;
        public int Intramuros;
        public Coordinates Coordinates;
        public int Womanprop;
        public string Imagepath;
        public string Website;
        public int Fees;
        public Socials Socials;
        public Resume Resume;

        public override string ToString()
        {
            string intra = Intramuros == 0 ? "Is not" : "Is";
            return $@"School n°{ID}: {Name}
Rank {Ranking} in {Category}s.
{intra} intramuros.
Located at {Coordinates.Latitude}, {Coordinates.Longitude}.
{Womanprop}% of student body is female.
Website: {Website}.
Estimated cost: {Fees}.
Socials: XXX
Summary: XXX";
        }
    }
}