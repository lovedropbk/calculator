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

        // NOTE:
        // These are thin wrappers delegating to GoalSeekViewModel, aligning with the architecture.
        // They also handle cancellation/exception paths and update Status for quick UI feedback.

        private async Task GoalSeekSolveForSubsidyAutoAsync()
        {
            if (GoalSeek == null)
            {
                Logger.Warn("GoalSeek VM not ready; ignoring Subsidy auto-run.");
                return;
            }

            try
            {
                Status = "Solving for subsidy...";
                Logger.Info($"[GoalSeek] Subsidy overlay clicked. Target={GoalSeek.TargetValue}");
                await GoalSeek.RunWithParamsAsync(GoalSeekVariable.UpfrontSubsidy, GoalSeekMetric.RoRAC, GoalSeek.TargetValue);
                Status = "Solved subsidy.";
                Logger.Info($"[GoalSeek] Subsidy solve completed. New SubsidyBudget={DealInput.SubsidyBudget}");
            }
            catch (OperationCanceledException)
            {
                Status = "Goal seek cancelled";
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GoalSeekSolveForSubsidyAutoAsync", ex);
                Status = $"Goal seek error: {ex.Message}";
            }
        }

        private async Task GoalSeekSolveForRateAutoAsync()
        {
            if (GoalSeek == null)
            {
                Logger.Warn("GoalSeek VM not ready; ignoring Rate auto-run.");
                return;
            }

            try
            {
                Status = "Solving for rate...";
                Logger.Info($"[GoalSeek] Rate overlay clicked. Target={GoalSeek.TargetValue}");
                await GoalSeek.RunWithParamsAsync(GoalSeekVariable.CustomerNominalRate, GoalSeekMetric.RoRAC, GoalSeek.TargetValue);
                Status = "Solved rate.";
                Logger.Info($"[GoalSeek] Rate solve completed. New CustomerNominalRate={DealInput.CustomerNominalRate}");
            }
            catch (OperationCanceledException)
            {
                Status = "Goal seek cancelled";
            }
            catch (Exception ex)
            {
                Logger.Error("Error in GoalSeekSolveForRateAutoAsync", ex);
                Status = $"Goal seek error: {ex.Message}";
            }
        }
    }
}