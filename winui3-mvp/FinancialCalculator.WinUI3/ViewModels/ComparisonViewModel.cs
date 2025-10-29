using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class ComparisonViewModel : ObservableObject
{
    public ObservableCollection<DealComparisonItemViewModel> ComparedDeals { get; } = new();

    // Campaign Designer collection (tiles shown in Campaign Designer tab)
    public System.Collections.ObjectModel.ObservableCollection<CampaignTileViewModel> DesignerCampaigns { get; } = new();

    private string _overallAvgRoRAC = "0.00%";
    public string OverallAvgRoRAC { get => _overallAvgRoRAC; set => SetProperty(ref _overallAvgRoRAC, value); }

    public ComparisonViewModel()
    {
        DesignerCampaigns.CollectionChanged += DesignerCampaigns_CollectionChanged;
    }

    private void DesignerCampaigns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (CampaignTileViewModel item in e.NewItems)
            {
                item.PropertyChanged += DesignerCampaign_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (CampaignTileViewModel item in e.OldItems)
            {
                item.PropertyChanged -= DesignerCampaign_PropertyChanged;
            }
        }
        RecalculateOverall();
    }

    private readonly DebounceDispatcher _volumeDebouncer = new();
    private bool _suppressVolumeNormalization = false;

    private void DesignerCampaign_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CampaignTileViewModel.CampaignVolumePct) || e.PropertyName == nameof(CampaignTileViewModel.AvgRoRAC))
        {
            if (e.PropertyName == nameof(CampaignTileViewModel.CampaignVolumePct))
            {
                _volumeDebouncer.Debounce(250, NormalizeCampaignVolumes);
            }
            RecalculateOverall();
        }
    }

    private void NormalizeCampaignVolumes()
    {
        if (_suppressVolumeNormalization) return;
        _suppressVolumeNormalization = true;

        try
        {
            var list = DesignerCampaigns.ToList();
            if (list.Count == 0) return;

            double total = list.Sum(c => c.CampaignVolumePct);

            if (Math.Abs(total - 100.0) < 1e-6 && total > 0) return;

            if (total <= 0)
            {
                double equal = Math.Round(100.0 / list.Count, 2);
                foreach (var c in list) c.CampaignVolumePct = equal;
            }
            else
            {
                double factor = 100.0 / total;
                foreach (var c in list) c.CampaignVolumePct = Math.Round(c.CampaignVolumePct * factor, 2);
            }

            double residual = 100.0 - list.Sum(c => c.CampaignVolumePct);
            if (Math.Abs(residual) >= 0.01 && list.Count > 0)
            {
                var max = list.OrderByDescending(c => c.CampaignVolumePct).First();
                max.CampaignVolumePct = Math.Round(max.CampaignVolumePct + residual, 2);
            }
        }
        finally
        {
            _suppressVolumeNormalization = false;
        }
    }

    private void RecalculateOverall()
    {
        double agg = 0.0;
        foreach (var c in DesignerCampaigns)
        {
            double vol = c.CampaignVolumePct;
            double avg = ParsePercentOrZero(c.AvgRoRAC);
            agg += avg * (vol / 100.0);
        }
        OverallAvgRoRAC = agg.ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double ParsePercentOrZero(string pctStr)
    {
        if (string.IsNullOrWhiteSpace(pctStr)) return 0.0;
        var clean = pctStr.Trim();
        if (clean.EndsWith("%")) clean = clean.Substring(0, clean.Length - 1);
        if (double.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return val / 100.0;
        return 0.0;
    }

    [RelayCommand]
    private void RemoveDeal(DealComparisonItemViewModel deal)
    {
        if (ComparedDeals.Contains(deal))
        {
            ComparedDeals.Remove(deal);
        }
    }

    [RelayCommand]
    private void ClearComparison()
    {
        ComparedDeals.Clear();
        DesignerCampaigns.Clear();
        RecalculateOverall();
    }
}

public partial class DealComparisonItemViewModel : ObservableObject
{
    public string Title { get; set; } = "Scenario";
    
    // Key Inputs
    public string VehicleName { get; set; } = "";
    public string Product { get; set; } = "";
    public string Price { get; set; } = "";
    public string DownPayment { get; set; } = "";
    public int Term { get; set; }
    public string NominalRate { get; set; } = "";
    public string FlatRate { get; set; } = "";
    public string Balloon { get; set; } = "";

    // Key Outputs
    public string MonthlyInstallment { get; set; } = "";
    public string FinancedAmount { get; set; } = "";
    public string TotalInterest { get; set; } = "";
    public string RoRAC { get; set; } = "";

    // Waterfall Data
    public ObservableCollection<WaterfallStepViewModel> WaterfallSteps { get; } = new();
}

public class WaterfallStepViewModel
{
    public string Label { get; set; } = "";
    public double Value { get; set; }
    public string FormattedValue { get; set; } = "";
    public bool IsTotal { get; set; }
    public string ColorHex { get; set; } = "#FF0078D7"; // Default blue
    public double HeightFactor { get; set; } // For UI relative sizing
}
// Campaign Designer view models
// Term-level breakdown for campaign (per-term RoRAC, customer rate and distribution)
public partial class TermBreakdownItemViewModel : ObservableObject
{
    public TermBreakdownItemViewModel() { }

    private int _term;
    public int Term
    {
        get => _term;
        set => SetProperty(ref _term, value);
    }

    public string TermLabel => $"{Term}m";

    private double _customerRatePct;
    /// <summary>
    /// Customer nominal rate in percent, e.g. 3.50 => "3.50%"
    /// </summary>
    public double CustomerRatePct
    {
        get => _customerRatePct;
        set => SetProperty(ref _customerRatePct, value);
    }

    private string _rorac = "0.00%";
    /// <summary>
    /// Per-term RoRAC formatted as percentage string (UI-friendly). Use CampaignTileViewModel.RecalculateAggregates to compute aggregated values.
    /// </summary>
    public string RoRAC
    {
        get => _rorac;
        set => SetProperty(ref _rorac, value);
    }

    private double _distributionPct = 0.0;
    /// <summary>
    /// Term distribution expressed as percentage of this campaign (0-100)
    /// </summary>
    public double DistributionPct
    {
        get => _distributionPct;
        set => SetProperty(ref _distributionPct, value);
    }
}

// Tile-level VM used in the Campaign Designer (one tile per campaign)
public partial class CampaignTileViewModel : ObservableObject
{
    private bool _suppressDistributionNormalization = false;

    public CampaignTileViewModel()
    {
        TermBreakdown.CollectionChanged += TermBreakdown_CollectionChanged;
    }

    private void TermBreakdown_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TermBreakdownItemViewModel item in e.NewItems)
            {
                item.PropertyChanged += TermItem_PropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (TermBreakdownItemViewModel item in e.OldItems)
            {
                item.PropertyChanged -= TermItem_PropertyChanged;
            }
        }
        // Ensure distributions are normalized and aggregates recalculated after structural changes
        NormalizeDistributions();
        RecalculateAggregates();
    }

    private void TermItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TermBreakdownItemViewModel.DistributionPct) && !_suppressDistributionNormalization)
        {
            NormalizeDistributions();
            RecalculateAggregates();
        }
        else if (e.PropertyName == nameof(TermBreakdownItemViewModel.RoRAC))
        {
            RecalculateAggregates();
        }
    }

    private void NormalizeDistributions()
    {
        _suppressDistributionNormalization = true;
        try
        {
            double total = TermBreakdown.Sum(t => t.DistributionPct);
            if (Math.Abs(total - 100.0) < 1e-6) return;

            if (total <= 0)
            {
                // If nothing set, place all weight on the first term (fallback)
                if (TermBreakdown.Count > 0)
                {
                    foreach (var t in TermBreakdown) t.DistributionPct = 0.0;
                    TermBreakdown[0].DistributionPct = 100.0;
                }
                return;
            }

            double factor = 100.0 / total;
            for (int i = 0; i < TermBreakdown.Count; i++)
            {
                TermBreakdown[i].DistributionPct = Math.Round(TermBreakdown[i].DistributionPct * factor, 2);
            }

            // Fix rounding residual
            double residual = 100.0 - TermBreakdown.Sum(t => t.DistributionPct);
            if (Math.Abs(residual) >= 0.01 && TermBreakdown.Count > 0)
            {
                var max = TermBreakdown.OrderByDescending(t => t.DistributionPct).First();
                max.DistributionPct = Math.Round(max.DistributionPct + residual, 2);
            }
        }
        finally
        {
            _suppressDistributionNormalization = false;
        }
    }


    private string _campaignId = string.Empty;
    public string CampaignId
    {
        get => _campaignId;
        set => SetProperty(ref _campaignId, value);
    }

    private string _title = "Campaign";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _product = string.Empty;
    public string Product
    {
        get => _product;
        set => SetProperty(ref _product, value);
    }

    // Per-term breakdown collection (editable by user in the UI)
    public System.Collections.ObjectModel.ObservableCollection<TermBreakdownItemViewModel> TermBreakdown { get; } = new();

    private double _campaignVolumePct = 0.0;
    /// <summary>
    /// Share of overall designer volume (0-100). Editable by user.
    /// </summary>
    public double CampaignVolumePct
    {
        get => _campaignVolumePct;
        set => SetProperty(ref _campaignVolumePct, value);
    }

    private string _avgRoRAC = "0.00%";
    /// <summary>
    /// Aggregated average RoRAC across terms, computed as sum(term_rorac * term_pct)
    /// </summary>
    public string AvgRoRAC
    {
        get => _avgRoRAC;
        set => SetProperty(ref _avgRoRAC, value);
    }

    /// <summary>
    /// Recalculate aggregated metrics from the per-term breakdown.
    /// AvgRoRAC is calculated as sum_over_terms(term_rorac_decimal * term_distribution_pct/100).
    /// </summary>
    public void RecalculateAggregates()
    {
        double sum = 0.0;
        foreach (var t in TermBreakdown)
        {
            double r = ParsePercentOrZero(t.RoRAC); // decimal (e.g. 0.0123)
            sum += r * (t.DistributionPct / 100.0);
        }
        AvgRoRAC = sum.ToString("0.00%", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static double ParsePercentOrZero(string pctStr)
    {
        if (string.IsNullOrWhiteSpace(pctStr)) return 0.0;
        var clean = pctStr.Trim();
        if (clean.EndsWith("%")) clean = clean.Substring(0, clean.Length - 1);
        if (double.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            return val / 100.0;
        }
        return 0.0;
    }
}