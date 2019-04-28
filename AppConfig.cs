using System;
namespace SendAPokemon
{
    public class AppConfig
    {
        public int IntervalType { get; set; }
        public int Interval { get; set; }
        public string WebHook { get; set; }
        public string PokemonUrl { get; set; }
    }

    public enum IntervalTypes
    {
        Seconds,
        Minutes,
        Hours,
        Days
    }
}
