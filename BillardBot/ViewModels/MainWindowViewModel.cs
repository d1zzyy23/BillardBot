using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BillardBot.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen = true;

    [ObservableProperty] 
    private ViewModelBase _currentPage = new HomeViewModel();

    [RelayCommand]
    private void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }
    
    [RelayCommand]
    private void ChangePage(string viewName)
    {
        CurrentPage = viewName switch
        {
            "Home" => new HomeViewModel(),
            "Tournament" => new TournamentViewModel(),
            _ => CurrentPage
        };
    }
}
