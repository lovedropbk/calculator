using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FinancialCalculator.Engine.Models.Facade;
using FinancialCalculator.WinUI3.Services;

namespace FinancialCalculator.WinUI3.ViewModels
{
    // MARK: Goal Seek Async Commands (explicit ICommand properties for XAML binding)
    public partial class MainViewModel
    {
        public IAsyncRelayCommand GoalSeekSolveForSubsidyAutoAsyncCommand { get; private set; } = null!;
        public IAsyncRelayCommand GoalSeekSolveForRateAutoAsyncCommand { get; private set; } = null!;

        // Initialize explicit async commands; invoked from MainViewModel constructor
        private void InitializeGoalSeekCommands()
        {
            GoalSeekSolveForSubsidyAutoAsyncCommand = new AsyncRelayCommand(GoalSeekSolveForSubsidyAutoAsync, CanRunGoalSeek);
            GoalSeekSolveForRateAutoAsyncCommand = new AsyncRelayCommand(GoalSeekSolveForRateAutoAsync, CanRunGoalSeek);
        }

        private bool CanRunGoalSeek()
        {
            // Enable when the GoalSeek VM exists and a target value is set
            return GoalSeek?.IsTargetSet == true;
        }

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
                Logger.Info($"[GoalSeek] {label} overlay clicked. Target={GoalSeek.TargetValue}");
                await GoalSeek.RunWithParamsAsync(variable, GoalSeekMetric.RoRAC, GoalSeek.TargetValue);
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

        // NOTE:
        // These are thin wrappers delegating to GoalSeekViewModel, aligning with the architecture.
        // They also handle cancellation/exception paths and update Status for quick UI feedback.

        private async Task GoalSeekSolveForSubsidyAutoAsync()
        {
            await RunGoalSeek(GoalSeekVariable.UpfrontSubsidy, "Solving for subsidy...", "Solved subsidy.", "Subsidy");
        }

        private async Task GoalSeekSolveForRateAutoAsync()
        {
            await RunGoalSeek(GoalSeekVariable.CustomerNominalRate, "Solving for rate...", "Solved rate.", "Rate");
        }
    }
}