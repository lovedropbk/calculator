using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels;

public partial class GoalSeekViewModel : ObservableObject
{
    private readonly FinancialFacade _financialFacade;
    private readonly DealInputViewModel _dealInput;
    private readonly Func<Task> _recalculateCallback;

    public GoalSeekViewModel(FinancialFacade financialFacade, DealInputViewModel dealInput, Func<Task> recalculateCallback)
    {
        _financialFacade = financialFacade;
        _dealInput = dealInput;
        _recalculateCallback = recalculateCallback;
    }

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _variableIndex = 0; // 0=Rate, 1=DownPayment, 2=Balloon(N/A), 3=Subsidy

    [ObservableProperty]
    private int _metricIndex = 0; // 0=Installment, 1=RoRAC

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyTargetSet))]
    private double _targetRoRac = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnyTargetSet))]
    private double _targetInstallment = 0;

    public bool IsAnyTargetSet => TargetRoRac > 0 || TargetInstallment > 0;
    public bool IsRoRacTargetSet => TargetRoRac > 0;
    public bool IsInstallmentTargetSet => TargetInstallment > 0;

    public (GoalSeekMetric metric, double value) ActiveTarget =>
        IsRoRacTargetSet ? (GoalSeekMetric.RoRAC, TargetRoRac) : (GoalSeekMetric.MonthlyInstallment, TargetInstallment);


    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private bool _isCalculating = false;

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        TargetRoRac = 0;
        TargetInstallment = 0;
        Status = "";
    }


    [RelayCommand]
    private async Task RunAsync()
    {
        var variable = VariableIndex switch
        {
            0 => GoalSeekVariable.CustomerNominalRate,
            1 => GoalSeekVariable.DownPaymentAmount,
            3 => GoalSeekVariable.UpfrontSubsidy,
             _ => throw new ArgumentOutOfRangeException(nameof(VariableIndex))
        };
        var (metric, value) = ActiveTarget;
        await RunWithParamsAsync(variable, metric, value);
    }


    public async Task RunWithParamsAsync(GoalSeekVariable variable, GoalSeekMetric metric, double targetValue)
    {
        try
        {
            IsCalculating = true;
            Status = $"Goal Seeking {variable}...";
            Logger.Info($"[GoalSeekVM] Start: variable={variable}, metric={metric}, target={targetValue}");
            await Task.Delay(10);

            var baseRequest = _dealInput.BuildScenarioRequest();
            
            double target = targetValue;
            if (metric == GoalSeekMetric.RoRAC) target /= 100.0;

            double result = _financialFacade.GoalSeek(baseRequest, variable, metric, target);

            if (variable == GoalSeekVariable.CustomerNominalRate)
            {
                _dealInput.CustomerNominalRate = Math.Round(result, 2);
            }
            else if (variable == GoalSeekVariable.DownPaymentAmount)
            {
                _dealInput.DownPaymentUnit = "THB";
                _dealInput.DownPaymentValueEntry = Math.Round(result, 0);
            }
            else if (variable == GoalSeekVariable.UpfrontSubsidy)
            {
                _dealInput.SubsidyBudget = Math.Round(result, 0);
            }

            Status = $"Goal Seek Complete. Result: {result:N2}";
            Logger.Info($"[GoalSeekVM] Completed: variable={variable}, result={result:N6}");
            await _recalculateCallback();
        }
        catch (Exception ex)
        {
             Status = $"Goal Seek Error: {ex.Message}";
             Logger.Error("[GoalSeekVM] Error during goal seek", ex);
        }
        finally
        {
            IsCalculating = false;
        }
    }

    public void OpenForRate()
    {
        IsOpen = true;
        VariableIndex = 0;
    }

    public void OpenForDownPayment()
    {
        IsOpen = true;
        VariableIndex = 1;
    }
}