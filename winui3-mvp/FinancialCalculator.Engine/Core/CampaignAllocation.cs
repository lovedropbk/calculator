using FinancialCalculator.Engine.Models.Facade;
using System;

namespace FinancialCalculator.Engine.Core
{
    /// <summary>
    /// Pure allocation logic for campaign subsidies with focus on SubDown.
    /// Ensures no double-counting: any subsidy used for SubDown is excluded from UpfrontSubsidies.
    /// </summary>
    public static class CampaignAllocation
    {
        public sealed record Input(
            decimal TransactionPrice,
            bool DownIsPercent,
            decimal DownValue,
            decimal TotalSubsidyBudget,
            decimal RequestedSubdownTHB
        );

        public sealed record Result(
            decimal BaseDownpayment,
            decimal SubsidyUsedForSubdown,
            decimal CustomerDownpayment,
            decimal SubsidyRemaining
        );

        /// <summary>
        /// Allocation rules:
        /// - base_downpayment computed on transaction price (after any cash discount) using current down input (THB or %).
        /// - subsidy_used_for_subdown = min(requested_subdown, total_subsidy_budget, base_downpayment).
        /// - customer_downpayment = max(0, base_downpayment - subsidy_used_for_subdown).
        /// - subsidy_remaining = total_subsidy_budget - subsidy_used_for_subdown.
        /// </summary>
        public static Result Allocate(Input i)
        {
            decimal baseDown = i.DownIsPercent ? i.TransactionPrice * i.DownValue / 100m : i.DownValue;
            if (baseDown < 0) baseDown = 0m;

            decimal want = Math.Max(0m, i.RequestedSubdownTHB);
            decimal budget = Math.Max(0m, i.TotalSubsidyBudget);

            decimal used = Math.Min(Math.Min(want, budget), baseDown);
            decimal customer = baseDown - used;
            decimal remaining = budget - used;

            return new Result(
                Decimal.Round(baseDown, 2),
                Decimal.Round(used, 2),
                Decimal.Round(customer, 2),
                Decimal.Round(remaining, 2)
            );
        }

        /// <summary>
        /// Apply allocation to a ScenarioRequest:
        /// - UpfrontSubsidies = subsidy_remaining (only portion not consumed by SubDown is eligible to be recognized)
        /// - SubdownValue = subsidy_used_for_subdown
        /// - SubdownIsPercent forced to false (absolute THB)
        /// </summary>
        public static ScenarioRequest ApplyToScenario(ScenarioRequest req, Result r)
            => req with
            {
                UpfrontSubsidies = r.SubsidyRemaining,
                SubdownIsPercent = false,
                SubdownValue = r.SubsidyUsedForSubdown
            };
    }
}