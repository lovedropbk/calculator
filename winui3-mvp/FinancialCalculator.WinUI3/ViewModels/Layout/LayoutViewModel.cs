using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels.Layout;

public sealed partial class LayoutViewModel : ObservableObject, IDisposable
{
    public PaneStateViewModel LeftPane { get; }
    public PaneStateViewModel RightPane { get; }

    public LayoutViewModel(DealInputViewModel dealInput, Func<bool> getRight, Action<bool> setRight)
    {
        LeftPane = new PaneStateViewModel(
            get: () => dealInput.IsDealInputsCollapsed,
            set: v => dealInput.IsDealInputsCollapsed = v,
            source: dealInput,
            propertyName: nameof(DealInputViewModel.IsDealInputsCollapsed));

        RightPane = new PaneStateViewModel(getRight, setRight);
    }

    public void Dispose()
    {
        LeftPane.Dispose();
    }
}
