using System.Collections.Generic;

namespace OvergownBot
{
    public class Player
    {
        public string[] SteamIds;
        public string[] IGNs;
    }
    
    public class PlayerDatabase
    {
        private Dictionary<string, Player> _players = new Dictionary<string, Player>();
    }
}