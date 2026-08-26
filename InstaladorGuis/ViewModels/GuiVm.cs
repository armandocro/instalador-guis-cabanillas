using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using FluentIcons.Common;
using InstaladorGuis.Helpers;
using InstaladorGuis.Models;

namespace InstaladorGuis.ViewModels;

internal sealed class GuiVm : INotifyPropertyChanged
{
    private bool _isChecked;
    private GuiStatus _status = GuiStatus.Unknown;
    private bool _isVisible = true;

    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public bool EsPrendas { get; init; }
    public string Chip => EsPrendas ? "PRC" : "PAQ";
    public Icon TypeIcon => EsPrendas ? Icon.ClothesHanger : Icon.Box;
    public Brush ChipBackground => EsPrendas ? BrushHelper.FromHex("#E8EDF5") : BrushHelper.FromHex("#EEE8E0");
    public Brush ChipForeground => EsPrendas ? BrushHelper.FromHex("#455A7A") : BrushHelper.FromHex("#6B5E4E");

    public bool IsChecked
    {
        get => _isChecked;
        set { _isChecked = value; OnPropertyChanged(); }
    }

    public GuiStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CardBackground));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(StatusLabelVisible));
            OnPropertyChanged(nameof(StatusLabelBrush));
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); }
    }

    public string StatusLabel => Status switch
    {
        GuiStatus.Installed => "instalada",
        GuiStatus.Checking => "procesando…",
        _ => ""
    };

    public Visibility StatusLabelVisible =>
        Status is GuiStatus.Installed or GuiStatus.Checking ? Visibility.Visible : Visibility.Collapsed;

    public Brush CardBackground => Status switch
    {
        GuiStatus.Installed => BrushHelper.FromHex("#EEF7EF"),
        GuiStatus.Checking => BrushHelper.FromHex("#FDF5EC"),
        _ => EsPrendas ? BrushHelper.FromHex("#F2F5FA") : BrushHelper.FromHex("#F7F3EE")
    };

    public Brush AccentBrush => Status switch
    {
        GuiStatus.Installed => BrushHelper.FromHex("#2F9E44"),
        GuiStatus.Checking => BrushHelper.FromHex("#E8862C"),
        GuiStatus.NotInstalled => BrushHelper.FromHex("#BFBFBF"),
        _ => BrushHelper.FromHex("#E6E6E6")
    };

    public Brush StatusLabelBrush => Status switch
    {
        GuiStatus.Installed => BrushHelper.FromHex("#2F9E44"),
        GuiStatus.Checking => BrushHelper.FromHex("#E8862C"),
        _ => BrushHelper.FromHex("#8C8C8C")
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
