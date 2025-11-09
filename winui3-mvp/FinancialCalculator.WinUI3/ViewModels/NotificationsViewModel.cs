using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class NotificationsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;

    public void ShowInfo(string message)
    {
        Severity = InfoBarSeverity.Informational;
        Message = message;
        IsOpen = true;
    }

    public void ShowWarning(string message)
    {
        Severity = InfoBarSeverity.Warning;
        Message = message;
        IsOpen = true;
    }

    public void ShowError(string message)
    {
        Severity = InfoBarSeverity.Error;
        Message = message;
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        Message = string.Empty;
    }
}
