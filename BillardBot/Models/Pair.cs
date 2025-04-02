using System.Collections.Generic;
using System.Linq;

namespace BillardBot.Models;

public class Pair
{
    public Dictionary<Player, int> PlayerNumbers;
    public Pair(Player firstPlayer, Player secondPlayer)
    {
        PlayerNumbers = new Dictionary<Player, int>
        {
            { firstPlayer, FirstPlayerNumber },
            { secondPlayer, SecondPlayerNumber }
        };
        
        firstPlayer.PositionsPlayed.Add(FirstPlayerNumber);
        secondPlayer.PositionsPlayed.Add(SecondPlayerNumber);
    }

    public int FirstPlayerNumber { get; set; }
    public int SecondPlayerNumber { get; set; }
    
     
    public Player FirstPlayer
    {
        get => PlayerNumbers.First().Key;
    }

    public Player SecondPlayer => PlayerNumbers.Last().Key;
}

public class Pair1 : Pair
{
    public Pair1(Player firstPlayer, Player secondPlayer) : base(firstPlayer, secondPlayer)
    {
        FirstPlayerNumber = 1;
        SecondPlayerNumber = 4;
    }
}
public class Pair2 : Pair
{
    public Pair2(Player firstPlayer, Player secondPlayer) : base(firstPlayer, secondPlayer)
    {
        FirstPlayerNumber = 2;
        SecondPlayerNumber = 5;
    }
}
public class Pair3 : Pair
{
    public Pair3(Player firstPlayer, Player secondPlayer) : base(firstPlayer, secondPlayer)
    {
        FirstPlayerNumber = 3;
        SecondPlayerNumber = 6;
    }
}