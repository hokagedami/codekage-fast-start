using System.ComponentModel;
using FastStart.Core.Models;
using Microsoft.UI.Xaml.Media;

namespace FastStart.UI.ViewModels;

public sealed class SearchResultViewModel : INotifyPropertyChanged
{
    private ImageSource? _icon;

    public AppInfo App { get; init; }
    public int Score { get; init; }
    public SearchMatchKind MatchKind { get; init; }
    public string HighlightedName { get; } // For showing match highlights
    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}