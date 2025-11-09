using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels.Exports;

public partial class ExportViewModel : ObservableObject
{
    private readonly ExportService _export;
    private readonly Func<(ViewModels.CampaignSummaryViewModel? active, FinancialCalculator.Engine.Models.Facade.ScenarioResult? res)> _getCurrent;
    private readonly Func<(double rateNominalPct, double commissionAmt, double idcOther)> _getInputs;
    private readonly Action<string> _setStatus;

    public ExportViewModel(ExportService export,
                           Func<(ViewModels.CampaignSummaryViewModel? active, FinancialCalculator.Engine.Models.Facade.ScenarioResult? res)> getCurrent,
                           Func<(double rateNominalPct, double commissionAmt, double idcOther)> getInputs,
                           Action<string> setStatus)
    {
        _export = export;
        _getCurrent = getCurrent;
        _getInputs = getInputs;
        _setStatus = setStatus;
        ExportXlsxCommand = new AsyncRelayCommand(ExportXlsxAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);
    }

    public IAsyncRelayCommand ExportXlsxCommand { get; }
    public IAsyncRelayCommand ExportPdfCommand { get; }

    private async Task ExportXlsxAsync()
    {
        try
        {
            _setStatus("Preparing export...");
            var (active, res) = _getCurrent();
            if (active == null || res == null)
            {
                _setStatus("No campaign or results to export.");
                return;
            }
            var (rateNominalPct, commissionAmt, idcOther) = _getInputs();
            var file = await _export.ExportScenarioAsync(active, res, rateNominalPct, commissionAmt, idcOther);
            _setStatus($"Exported XLSX to {file}");
        }
        catch (Exception ex)
        {
            _setStatus($"Export failed: {ex.Message}");
        }
    }

    private async Task ExportPdfAsync()
    {
        try
        {
            _setStatus("Preparing PDF export...");
            await Task.Delay(10);
            _setStatus("PDF export not yet implemented.");
        }
        catch (Exception ex)
        {
            _setStatus($"Export PDF failed: {ex.Message}");
        }
    }
}
