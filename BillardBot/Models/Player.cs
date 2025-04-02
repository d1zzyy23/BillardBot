using System.Collections.Generic;

namespace BillardBot.Models;

public class Player
{
    public string Name { get; set; }
    public HashSet<int> PositionsPlayed { get; set; }
    public List<int> MatchHistory { get; set; }

    
    public Player(string name)
    {
        Name = name;
        PositionsPlayed = new HashSet<int>();
        MatchHistory = new List<int>();
    }
}