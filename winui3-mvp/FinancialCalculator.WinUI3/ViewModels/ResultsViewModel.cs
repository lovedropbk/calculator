using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class ResultsViewModel : ObservableObject
{
    // Metrics
    [ObservableProperty] private MetricsViewModel _metrics = new();

    // Cashflows
    public ObservableCollection<CashflowRowViewModel> Cashflows { get; } = new();

    // Profitability Waterfall Data
    [ObservableProperty] private double _wfCustomerRate;
    [ObservableProperty] private double _wfDealIRRNominal;
    [ObservableProperty] private double _wfCostOfDebtMatched;
    [ObservableProperty] private double _wfCostOfCreditRisk;
    [ObservableProperty] private double _wfOPEX;
    [ObservableProperty] private double _wfNetInterestMargin;
    [ObservableProperty] private double _wfNetEBITMargin;

    // Cashflow Summaries
    [ObservableProperty] private string _totalPrincipalPaid = "0";
    [ObservableProperty] private string _totalInterestPaid = "0";
    [ObservableProperty] private string _totalFeesPaid = "0";
    [ObservableProperty] private string _totalPayments = "0";
    [ObservableProperty] private string _netAmountFinanced = "0";
    [ObservableProperty] private string _cashflowCampaignName = "";
}