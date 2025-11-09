using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FinancialCalculator.WinUI3.Controls;

public sealed partial class PaneToggleButton : UserControl, INotifyPropertyChanged
{
    public PaneToggleButton()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(PaneToggleButton), new PropertyMetadata(false, OnPanePropChanged));

    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    public static readonly DependencyProperty ToggleCommandProperty =
        DependencyProperty.Register(nameof(ToggleCommand), typeof(ICommand), typeof(PaneToggleButton), new PropertyMetadata(null));

    public ICommand ToggleCommand
    {
        get => (ICommand)GetValue(ToggleCommandProperty);
        set => SetValue(ToggleCommandProperty, value);
    }

    public static readonly DependencyProperty IsRightSideProperty =
        DependencyProperty.Register(nameof(IsRightSide), typeof(bool), typeof(PaneToggleButton), new PropertyMetadata(false, OnPanePropChanged));

    public bool IsRightSide
    {
        get => (bool)GetValue(IsRightSideProperty);
        set => SetValue(IsRightSideProperty, value);
    }

    public bool ShowLeftGlyph => IsRightSide ? IsCollapsed : !IsCollapsed;
    public bool ShowRightGlyph => IsRightSide ? !IsCollapsed : IsCollapsed;
    public string TooltipText => IsCollapsed ? "Expand panel" : "Collapse panel";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static void OnPanePropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PaneToggleButton c)
        {
            c.Notify(nameof(IsCollapsed));
            c.Notify(nameof(IsRightSide));
            c.Notify(nameof(ShowLeftGlyph));
            c.Notify(nameof(ShowRightGlyph));
            c.Notify(nameof(TooltipText));
        }
    }
}
