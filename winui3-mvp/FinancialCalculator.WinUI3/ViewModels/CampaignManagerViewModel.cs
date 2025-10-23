using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class CampaignManagerViewModel : ObservableObject
{
    public ObservableCollection<CampaignSummaryViewModel> StandardCampaigns { get; } = new();
    public ObservableCollection<CampaignSummaryViewModel> MyCampaigns { get; } = new();

    private CampaignSummaryViewModel? _selectedCampaign;
    public CampaignSummaryViewModel? SelectedCampaign
    {
        get => _selectedCampaign;
        set
        {
            if (SetProperty(ref _selectedCampaign, value))
            {
                if (value != null && _selectedMyCampaign != null)
                {
                    SelectedMyCampaign = null;
                }
                OnPropertyChanged(nameof(ActiveCampaign));
            }
        }
    }

    private CampaignSummaryViewModel? _selectedMyCampaign;
    public CampaignSummaryViewModel? SelectedMyCampaign
    {
        get => _selectedMyCampaign;
        set
        {
            if (SetProperty(ref _selectedMyCampaign, value))
            {
                if (value != null && _selectedCampaign != null)
                {
                    SelectedCampaign = null;
                }
                OnPropertyChanged(nameof(ActiveCampaign));
            }
        }
    }

    public CampaignSummaryViewModel? ActiveCampaign => SelectedMyCampaign ?? SelectedCampaign;

    [RelayCommand]
    private void CopyToMyCampaigns(CampaignSummaryViewModel? item)
    {
         // Implementation needs access to MainViewModel or similar to trigger refreshes... 
         // This suggests CampaignManager might need a reference to parent or use events.
    }

    // ... other campaign management logic
}