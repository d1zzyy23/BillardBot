using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BillardBot.Models;

public class Match
{
    public Pair Pair1 { get; set; }
    public Pair Pair2 { get; set; }
    public Pair Pair3 { get; set; }

    public int Number { get; set; }

    public Match(Pair pair1, Pair pair2, Pair pair3, int number)
    {
        Pair1 = pair1;
        Pair2 = pair2;
        Pair3 = pair3;
        Number = number;
        
        Debug.WriteLine($"Match Number: {Number}");
        Debug.WriteLine($"Pair 1: {Pair1Players}");
        Debug.WriteLine($"Pair 2: {Pair2Players}");
        Debug.WriteLine($"Pair 3: {Pair3Players}");
    }

    public string MatchNumber => $"Match: {Number.ToString()}";
    public string Pair1Players => $"{Pair1.FirstPlayerNumber} {Pair1.FirstPlayer.Name} - {Pair1.SecondPlayer.Name} {Pair1.SecondPlayerNumber}";
    public string Pair2Players => $"{Pair2.FirstPlayerNumber} {Pair2.FirstPlayer.Name} - {Pair2.SecondPlayer.Name} {Pair2.SecondPlayerNumber}";
    public string Pair3Players => $"{Pair3.FirstPlayerNumber} {Pair3.FirstPlayer.Name} - {Pair3.SecondPlayer.Name} {Pair3.SecondPlayerNumber}";
    
    
    public void ConfirmOrder()
    {
        // List of pairs
        var pairs = new List<Pair> { Pair1, Pair2, Pair3 };

        foreach (var pair in pairs)
        {
            // Iterate through the players in the pair
            foreach (var player in pair.PlayerNumbers.Keys.ToList())  // ToList to avoid modifying collection during iteration
            {
                var position = pair.PlayerNumbers[player]; // Get the player's current position in the pair

                if (player.PositionsPlayed.Contains(position)) // Check if the player has already played this position
                {
                    var partner = pair.PlayerNumbers.First(p => p.Key != player).Key;  // Find the partner in the pair

                    if (partner.PositionsPlayed.Contains(position))  // If the partner has also played this position
                    {
                        // Try to swap with another pair
                        bool switched = false;

                        foreach (var otherPair in pairs.Where(p => p != pair))
                        {
                            var otherPosition = otherPair.PlayerNumbers.Values.FirstOrDefault(p => !otherPair.PlayerNumbers.Keys.Any(pl => pl.PositionsPlayed.Contains(p)));

                            if (otherPosition != 0)
                            {
                                // Perform the swap
                                var newPartner = otherPair.PlayerNumbers.First(p => !p.Key.PositionsPlayed.Contains(otherPosition)).Key;
                                SwapPositions(player, newPartner);
                                switched = true;
                                break;
                            }
                        }

                        if (!switched)
                        {
                            // If no switch works, randomize the order in the pair
                            RandomizePairOrder(pair);
                        }
                    }
                    else
                    {
                        // Swap positions within the pair if the partner hasn't played this position
                        SwapPositions(player, partner);
                    }

                    // Add the current position to the player's played positions
                    player.PositionsPlayed.Add(position);
                    break;  // Exit once we've processed the player for this pair
                }
            }
        }
    }

    // Helper method to swap players between two positions
    private void SwapPositions(Player player1, Player player2)
    {
        var tempPositions = new HashSet<int>(player1.PositionsPlayed);
        player1.PositionsPlayed = new HashSet<int>(player2.PositionsPlayed);
        player2.PositionsPlayed = tempPositions;
    }

    // Helper method to randomize the order of a pair
    private void RandomizePairOrder(Pair pair)
    {
        var random = new Random();
        var playerList = pair.PlayerNumbers.Keys.ToList();
        int index1 = random.Next(playerList.Count);
        int index2;
        do
        {
            index2 = random.Next(playerList.Count);
        } while (index1 == index2);

        // Swap the players randomly
        (playerList[index1], playerList[index2]) = (playerList[index2], playerList[index1]);
    }
}