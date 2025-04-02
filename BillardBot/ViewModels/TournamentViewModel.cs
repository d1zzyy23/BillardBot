using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using BillardBot.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Match = BillardBot.Models.Match;

namespace BillardBot.ViewModels;

public partial class TournamentViewModel : ViewModelBase
{
    [ObservableProperty] private int _matchesNumber;
    private ObservableCollection<Player> Players;
    private ObservableCollection<Pair> Pairs;
    private ObservableCollection<(Pair, int)> MatchedPairs;
    public ObservableCollection<Match> Matches { get; set; } = new ObservableCollection<Match>();

    [ObservableProperty] private int _defaultMatchCount = 3;
    [ObservableProperty] private int _teamSize = 2;
    [ObservableProperty] private int _teamsPerMatch = 3;

    private readonly Random _random = new Random();

    public TournamentViewModel()
    {
        Players = new ObservableCollection<Player>
        {
            new Player("Frederik"),
            new Player("Amalie"),
            new Player("Mikkel"),
            new Player("Sofie"),
            new Player("Kasper"),
            new Player("Emma"),
            new Player("Magnus"),
            new Player("Clara"),
            new Player("Andreas"),
            new Player("Mathilde"),
            new Player("Nikolaj"),
            new Player("Julie"),
            new Player("Rasmus"),
            new Player("Cecilie")
        };
    }

    [RelayCommand]
    private void Generate()
    {
        MatchesNumber = CalculateMatchesCount();
        Pairs = GeneratePairs();
        MatchedPairs = AssignMatchNumberAndTurn(Pairs);
    }

    private int CalculateMatchesCount()
    {
        int actualMatches = DefaultMatchCount;

        while (Players.Count * actualMatches % (TeamSize * TeamsPerMatch) != 0)
        {
            actualMatches++;
        }

        return actualMatches;
    }

    private ObservableCollection<Pair> GeneratePairs()
    {
        var pairs = new ObservableCollection<Pair>();
        Player carryOverPlayer = null;

        Debug.WriteLine($"Generating pairs for {MatchesNumber} rounds...");

        for (int x = 1; x <= MatchesNumber; x++)
        {
            var usedPairs = new HashSet<Pair>(); // Track used pairs
            var tempList = new List<Player>(Players);

            if (carryOverPlayer != null)
            {
                tempList.Add(carryOverPlayer);
            }

            Debug.WriteLine($"Round {x}: Starting with {tempList.Count} players...");

            while (tempList.Count > 1)
            {
                bool canAdd;
                Player pairing;
                Player player;
                Pair pair;
                do
                {
                    int attempts = 0;
                    int maxAttempts = 1000;
                    player = tempList[_random.Next(tempList.Count)];
                    pairing = tempList[_random.Next(tempList.Count)];

                    while (player == pairing)
                    {
                        pairing = tempList[_random.Next(tempList.Count)];
                    }

                    pair = new Pair(player, pairing);
                    canAdd = !usedPairs.Contains(pair);

                    attempts++;
                    if (attempts > maxAttempts)
                    {
                        Debug.WriteLine("Unable to generate a unique pair.");
                        GeneratePairs(); // Retry if we can't generate a unique pair
                    }
                } while (!canAdd);

                usedPairs.Add(pair);
                pairs.Add(pair);

                Debug.WriteLine(
                    $"Pairing: ({string.Join(", ", pair.PlayerNumbers.Keys.Select(p => p.Name))}), Round {x}");

                tempList.Remove(player);
                tempList.Remove(pairing);
            }

            if (tempList.Count == 1)
            {
                carryOverPlayer = tempList[0];
                Debug.WriteLine($"Carryover: {carryOverPlayer.Name} will move to Round {x + 1}");
            }
            else
            {
                carryOverPlayer = null; // Reset carryover if the count is even
            }
        }

        return pairs;
    }

    private ObservableCollection<(Pair, int)> AssignMatchNumberAndTurn(ObservableCollection<Pair> pairs)
    {
        var matchedPairs = new ObservableCollection<(Pair, int)>();
        var matchNumber = 1;
        var matches = new ObservableCollection<Match>();
        var originalPairs = new ObservableCollection<Pair>(pairs);

        while (pairs.Count > 0)
        {
            var matchPairs = new List<Pair>();
            var playersAssigned = new HashSet<Player>();

            var validPairs = new List<Pair>();
            while (matchPairs.Count < 3 && pairs.Count > 0)
            {
                // Pre-filter pairs that meet criteria
                validPairs = pairs
                    .Where(p => p.PlayerNumbers.All(pp => !pp.Key.MatchHistory.Contains(matchNumber - 1)) &&
                                p.PlayerNumbers.All(pp => !playersAssigned.Contains(pp.Key)))
                    .ToList();

                // Relax condition if no valid pairs found
                if (validPairs.Count == 0)
                {
                    Debug.WriteLine("Some players have played the last match.");
                    validPairs = pairs
                        .Where(p => p.PlayerNumbers.Any(pp => !pp.Key.MatchHistory.Contains(matchNumber - 1)) &&
                                    p.PlayerNumbers.All(pp => !playersAssigned.Contains(pp.Key)))
                        .ToList();
                }

                if (validPairs.Count == 0)
                {
                    Debug.WriteLine("No valid pairs left.");
                    validPairs = pairs
                        .Where(p => p.PlayerNumbers.All(pp => !playersAssigned.Contains(pp.Key)))
                        .ToList();
                }

                if (validPairs.Count == 0)
                {
                    Debug.WriteLine("No valid pairs available, retrying...");
                    return AssignMatchNumberAndTurn(originalPairs); // Properly return from recursion
                }

                var pair = validPairs[_random.Next(validPairs.Count)];
                matchPairs.Add(pair);
                pairs.Remove(pair);

                foreach (var player in pair.PlayerNumbers.Keys)
                {
                    player.MatchHistory.Add(matchNumber);
                    playersAssigned.Add(player);
                }
            }

            if (matchPairs.Count == 3)
            {
                // Create Pair1, Pair2, Pair3 for positions (1,4), (2,5), (3,6)
                var pair1 = new Pair1(matchPairs[0].FirstPlayer, matchPairs[0].SecondPlayer);
                var pair2 = new Pair2(matchPairs[1].FirstPlayer, matchPairs[1].SecondPlayer);
                var pair3 = new Pair3(matchPairs[2].FirstPlayer, matchPairs[2].SecondPlayer);

                // First try to arrange pairs to avoid position conflicts (especially position 1)
                OptimizeInitialPairAssignment(new List<Pair> { pair1, pair2, pair3 });

                // Then optimize player positions within each pair
                OptimizePlayerPositionsInPairs(pair1);
                OptimizePlayerPositionsInPairs(pair2);
                OptimizePlayerPositionsInPairs(pair3);

                // Check if there are still conflicts in position 1 - try swapping entire pairs as last resort
                if (HasPosition1Conflicts(new List<Pair> { pair1, pair2, pair3 }))
                {
                    AttemptSwapPairsForPosition1(new List<Pair> { pair1, pair2, pair3 });
                }

                // Update position history for all players
                TrackPositionsPlayed(pair1);
                TrackPositionsPlayed(pair2);
                TrackPositionsPlayed(pair3);

                // After optimization, add the match with the updated pairs
                matches.Add(new Match(pair1, pair2, pair3, matchNumber));
                matchedPairs.Add((pair1, matchNumber));
                matchedPairs.Add((pair2, matchNumber));
                matchedPairs.Add((pair3, matchNumber));

                Debug.WriteLine($"Match {matchNumber} added.");

                matchNumber++;
            }

            Matches.Clear(); // Clear current collection

            foreach (var match in matches)
            {
                Matches.Add(match); // Add new matches to existing collection
            }

            Debug.WriteLine($"Matches Count: {Matches.Count}");
        }

        // Log the matched pairs for debugging
        foreach (var (pair, matchNum) in matchedPairs)
        {
            Debug.WriteLine(
                $"Match {matchNum}: ({string.Join(", ", pair.PlayerNumbers.Keys.Select(p => p.Name))})");
        }

        return matchedPairs;
    }

// Optimize which pair is assigned to which position set (1&4, 2&5, 3&6)
    private void OptimizeInitialPairAssignment(List<Pair> pairs)
    {
        Debug.WriteLine("Optimizing initial pair assignment...");

        // Try all possible arrangements of the 3 pairs
        var bestArrangement = new List<Pair>(pairs);
        int minConflicts = CountPositionConflicts(pairs);

        // Try all permutations of 3 pairs (there are 6 possibilities)
        var arrangements = GetAllPermutations(pairs);

        foreach (var arrangement in arrangements)
        {
            // Need to swap the actual types to match Pair1, Pair2, Pair3
            var testArrangement = new List<Pair>(arrangement);
            int conflicts = CountPositionConflicts(testArrangement);

            // Prioritize arrangements with fewer position 1 conflicts
            int position1Conflicts = CountPosition1Conflicts(testArrangement);

            if (position1Conflicts == 0 && conflicts < minConflicts)
            {
                minConflicts = conflicts;
                bestArrangement = new List<Pair>(testArrangement);

                // If we found a perfect arrangement with no position 1 conflicts, use it
                if (position1Conflicts == 0)
                {
                    break;
                }
            }
        }

        // Apply the best arrangement by swapping pair contents
        for (int i = 0; i < pairs.Count; i++)
        {
            if (pairs[i] != bestArrangement[i])
            {
                // Find the corresponding pair in the original list and swap
                int swapIndex = pairs.IndexOf(bestArrangement[i]);
                if (swapIndex != -1 && swapIndex != i)
                {
                    SwapEntirePairs(pairs[i], pairs[swapIndex]);
                }
            }
        }

        Debug.WriteLine("Initial pair assignment optimized.");
    }

// Generate all permutations of pairs
    private List<List<Pair>> GetAllPermutations(List<Pair> pairs)
    {
        var result = new List<List<Pair>>();

        // For 3 pairs, we can just hard-code the 6 permutations
        result.Add(new List<Pair> { pairs[0], pairs[1], pairs[2] });
        result.Add(new List<Pair> { pairs[0], pairs[2], pairs[1] });
        result.Add(new List<Pair> { pairs[1], pairs[0], pairs[2] });
        result.Add(new List<Pair> { pairs[1], pairs[2], pairs[0] });
        result.Add(new List<Pair> { pairs[2], pairs[0], pairs[1] });
        result.Add(new List<Pair> { pairs[2], pairs[1], pairs[0] });

        return result;
    }

// Count total position conflicts
    private int CountPositionConflicts(List<Pair> pairs)
    {
        int conflicts = 0;

        // Check each pair and count conflicts
        foreach (var pair in pairs)
        {
            // Check first player
            if (pair.FirstPlayer.PositionsPlayed.Contains(pair.FirstPlayerNumber))
            {
                conflicts++;
            }

            // Check second player
            if (pair.SecondPlayer.PositionsPlayed.Contains(pair.SecondPlayerNumber))
            {
                conflicts++;
            }
        }

        return conflicts;
    }

// Count just position 1 conflicts (highest priority)
    private int CountPosition1Conflicts(List<Pair> pairs)
    {
        int conflicts = 0;

        // Find the pair that would be assigned to position 1
        var position1Pair = pairs.FirstOrDefault(p => p is Pair1);

        if (position1Pair != null)
        {
            // Check first player in position 1
            if (position1Pair.FirstPlayerNumber == 1 &&
                position1Pair.FirstPlayer.PositionsPlayed.Contains(1))
            {
                conflicts++;
            }

            // Check second player in position 1
            if (position1Pair.SecondPlayerNumber == 1 &&
                position1Pair.SecondPlayer.PositionsPlayed.Contains(1))
            {
                conflicts++;
            }
        }

        return conflicts;
    }

// Check if there are any position 1 conflicts
    private bool HasPosition1Conflicts(List<Pair> pairs)
    {
        return CountPosition1Conflicts(pairs) > 0;
    }

// Try to swap pairs specifically to avoid position 1 conflicts
    private void AttemptSwapPairsForPosition1(List<Pair> pairs)
    {
        Debug.WriteLine("Attempting to resolve position 1 conflicts...");

        // Find the pair assigned to position 1 (should be a Pair1)
        var position1Pair = pairs.FirstOrDefault(p => p is Pair1);

        if (position1Pair == null) return;

        // If there's a conflict in position 1
        if (position1Pair.FirstPlayerNumber == 1 && position1Pair.FirstPlayer.PositionsPlayed.Contains(1) ||
            position1Pair.SecondPlayerNumber == 1 && position1Pair.SecondPlayer.PositionsPlayed.Contains(1))
        {
            // Try to find another pair to swap with
            foreach (var otherPair in pairs.Where(p => p != position1Pair))
            {
                bool otherPairWorks = true;

                // Check if other pair's players would conflict in position 1
                if (otherPair.FirstPlayer.PositionsPlayed.Contains(1) &&
                    otherPair.SecondPlayer.PositionsPlayed.Contains(1))
                {
                    otherPairWorks = false;
                }

                // Check if position1Pair's players would conflict in other pair's position
                if (position1Pair.FirstPlayer.PositionsPlayed.Contains(otherPair.FirstPlayerNumber) &&
                    position1Pair.SecondPlayer.PositionsPlayed.Contains(otherPair.SecondPlayerNumber))
                {
                    otherPairWorks = false;
                }

                if (otherPairWorks)
                {
                    SwapEntirePairs(position1Pair, otherPair);
                    Debug.WriteLine("Successfully swapped pairs to resolve position 1 conflict.");
                    return;
                }
            }
        }

        Debug.WriteLine("Could not resolve position 1 conflicts by swapping pairs.");
    }

// Optimize player positions within each pair
    private void OptimizePlayerPositionsInPairs(Pair pair)
    {
        // Check if either player has played in their assigned position before
        bool firstPlayerConflict = pair.FirstPlayer.PositionsPlayed.Contains(pair.FirstPlayerNumber);
        bool secondPlayerConflict = pair.SecondPlayer.PositionsPlayed.Contains(pair.SecondPlayerNumber);

        // If both have conflicts or neither has conflicts, no advantage to swapping
        if (firstPlayerConflict && !secondPlayerConflict)
        {
            // First player has conflict but second doesn't - swap
            SwapPlayers(pair);
            Debug.WriteLine($"Swapped players in pair to avoid position conflict.");
        }
        else if (!firstPlayerConflict && secondPlayerConflict)
        {
            // Second player has conflict but first doesn't - swap
            SwapPlayers(pair);
            Debug.WriteLine($"Swapped players in pair to avoid position conflict.");
        }
        else if (firstPlayerConflict && secondPlayerConflict)
        {
            // Both have conflicts - if position 1 is involved, prioritize it
            if (pair.FirstPlayerNumber == 1 || pair.SecondPlayerNumber == 1)
            {
                // Swap if it helps position 1
                if ((pair.FirstPlayerNumber == 1 && !pair.SecondPlayer.PositionsPlayed.Contains(1)) ||
                    (pair.SecondPlayerNumber == 1 && !pair.FirstPlayer.PositionsPlayed.Contains(1)))
                {
                    SwapPlayers(pair);
                    Debug.WriteLine($"Swapped players to prioritize position 1.");
                }
            }
        }
    }

// Track positions played by both players in a pair
    private void TrackPositionsPlayed(Pair pair)
    {
        pair.FirstPlayer.PositionsPlayed.Add(pair.FirstPlayerNumber);
        pair.SecondPlayer.PositionsPlayed.Add(pair.SecondPlayerNumber);

        Debug.WriteLine(
            $"Position tracking: {pair.FirstPlayer.Name}({pair.FirstPlayerNumber}), {pair.SecondPlayer.Name}({pair.SecondPlayerNumber})");
    }

// Swap players within a pair (keeping the pair together)
    private void SwapPlayers(Pair pair)
    {
        // Swap the players in the dictionary based on their position numbers
        var firstPlayer = pair.PlayerNumbers.Keys.First();
        var secondPlayer = pair.PlayerNumbers.Keys.Last();

        // Swap their positions in the dictionary
        pair.PlayerNumbers.Clear();

        // Add them back with swapped positions
        pair.PlayerNumbers.Add(firstPlayer, pair.SecondPlayerNumber);
        pair.PlayerNumbers.Add(secondPlayer, pair.FirstPlayerNumber);

        Debug.WriteLine($"Swapped players within pair: {firstPlayer.Name} and {secondPlayer.Name}");
    }

// Swap entire pairs while preserving the pair integrity
    private void SwapEntirePairs(Pair pair1, Pair pair2)
    {
        Debug.WriteLine($"Swapping entire pairs");

        // Store original players
        var pair1First = pair1.FirstPlayer;
        var pair1Second = pair1.SecondPlayer;
        var pair2First = pair2.FirstPlayer;
        var pair2Second = pair2.SecondPlayer;

        // Get the positions (these stay with the pair objects)
        var pair1FirstPos = pair1.FirstPlayerNumber;
        var pair1SecondPos = pair1.SecondPlayerNumber;
        var pair2FirstPos = pair2.FirstPlayerNumber;
        var pair2SecondPos = pair2.SecondPlayerNumber;

        // Clear the player dictionaries
        pair1.PlayerNumbers.Clear();
        pair2.PlayerNumbers.Clear();

        // Assign the swapped players
        pair1.PlayerNumbers.Add(pair2First, pair1FirstPos);
        pair1.PlayerNumbers.Add(pair2Second, pair1SecondPos);

        pair2.PlayerNumbers.Add(pair1First, pair2FirstPos);
        pair2.PlayerNumbers.Add(pair1Second, pair2SecondPos);

        Debug.WriteLine(
            $"Pairs swapped: {pair2First.Name}/{pair2Second.Name} with {pair1First.Name}/{pair1Second.Name}");
    }
}