using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels
{
    public partial class MainViewModel
    {
        public IAsyncRelayCommand GoalSeekSolveForSubsidyAutoAsyncCommand { get; private set; } = null!;
        public IAsyncRelayCommand GoalSeekSolveForRateAutoAsyncCommand { get; private set; } = null!;
        public IAsyncRelayCommand GoalSeekSolveForDownPaymentAutoAsyncCommand { get; private set; } = null!;

        private void InitializeGoalSeekCommands()
        {
            GoalSeekSolveForSubsidyAutoAsyncCommand = new AsyncRelayCommand(GoalSeekSolveForSubsidyAutoAsync, canExecute: CanRunGoalSeek);
            GoalSeekSolveForRateAutoAsyncCommand = new AsyncRelayCommand(GoalSeekSolveForRateAutoAsync, canExecute: CanRunGoalSeek);
            GoalSeekSolveForDownPaymentAutoAsyncCommand = new AsyncRelayCommand(GoalSeekSolveForDownPaymentAutoAsync, canExecute: CanRunGoalSeek);
        }

        private bool CanRunGoalSeek() => GoalSeek?.IsAnyTargetSet == true;

        private async Task RunGoalSeek(GoalSeekVariable variable, string startStatus, string successStatus, string label)
        {
            if (GoalSeek == null)
            {
                Logger.Warn("GoalSeek VM not ready; ignoring " + label + " auto-run.");
                return;
            }

            try
            {
                Status = startStatus;
                var t = GoalSeek.ActiveTarget;
                Logger.Info($"[GoalSeek] {label} overlay clicked. TargetMetric={t.metric}, Target={t.value}");
                await GoalSeek.RunWithParamsAsync(variable, t.metric, t.value);
                Status = successStatus;
                Logger.Info($"[GoalSeek] {label} solve completed.");
            }
            catch (OperationCanceledException)
            {
                Status = "Goal seek cancelled";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in RunGoalSeek ({label})", ex);
                Status = $"Goal seek error: {ex.Message}";
            }
        }

        private async Task GoalSeekSolveForSubsidyAutoAsync()
        {
            await RunGoalSeek(GoalSeekVariable.UpfrontSubsidy, "Solving for subsidy...", "Solved subsidy.", "Subsidy");
        }

        private async Task GoalSeekSolveForRateAutoAsync()
        {
            await RunGoalSeek(GoalSeekVariable.CustomerNominalRate, "Solving for rate...", "Solved rate.", "Rate");
        }

        private async Task GoalSeekSolveForDownPaymentAutoAsync()
        {
            await RunGoalSeek(GoalSeekVariable.DownPaymentAmount, "Solving for down payment...", "Solved down payment.", "Down Payment");
        }
    }
}
