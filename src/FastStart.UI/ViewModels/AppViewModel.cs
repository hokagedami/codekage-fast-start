using System.ComponentModel;
using FastStart.Core.Models;
using Microsoft.UI.Xaml.Media;

namespace FastStart.UI.ViewModels;

public sealed class AppViewModel : INotifyPropertyChanged
{
    private ImageSource? _icon;
    private bool _isPinned;

    public required AppInfo App { get; init; }

    public ImageSource? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
        }
    }

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned != value)
            {
                _isPinned = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
            }
        }
    }

    public int PinOrder { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
}