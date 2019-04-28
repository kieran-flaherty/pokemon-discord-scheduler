namespace SendAPokemon
{
    public class Pokemon
    {
        public string Name { get; set; }
        public string Sprite {get; set; }

        public Pokemon(string name, string sprite)
        {
            Name = name;
            Sprite = sprite;
        }
    }
}