using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BillardBot.ViewModels;

public partial class TournamentViewModel : ViewModelBase
{
    public TournamentViewModel()
    {
    }

    [RelayCommand]
    private void Generate()
    {
        GenerateBracket();
    }

    private void GenerateBracket()
    {
       
    }

    private List<string> GetAllPlayerNames()
    {
        return new List<string>
        {
            "Frederik", "Amalie", "Mikkel", "Sofie", "Kasper", "Emma",
            "Magnus", "Clara", "Andreas", "Mathilde", "Nikolaj",
            "Julie", "Rasmus", "Cecilie", "Jakob", "Laura", "Tobias"
        };
    }
}
