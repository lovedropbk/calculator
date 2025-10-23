# Integrated Deal Comparison & Profitability Waterfall Design

## Overview
A new tab "Comparison" will be added to the main application window. This tab allows users to compare multiple deal scenarios side-by-side, including their full profitability waterfall.

## UI Layout

### Comparison Tab
- **Top Bar**: Controls to add current deal to comparison, clear comparison, and export.
- **Main Area**: A horizontal scrollable area containing "Deal Cards".
- **Deal Card**: A vertical column representing one deal scenario.
    - **Header**: Deal Name (e.g., "Scenario A", editable), Delete button.
    - **Key Inputs Summary**: Vehicle, Product, Price, Down Payment, Term, Rate.
    - **Key Outputs Summary**: Monthly Installment, Flat Rate, Financed Amount.
    - **Profitability Waterfall (Graphical)**: A simplified waterfall chart showing:
        - Customer Rate (starting point)
        - - Cost of Funds
        - - Cost of Risk
        - - OPEX
        - +/- Subsidies/IDCs (net)
        - = RoRAC (or Net Margin)
    - **Detailed Metrics (Collapsible)**: Full list of waterfall metrics (Deal IRR, NIM, etc.) similar to current details panel.

### Waterfall Chart Implementation
- Since WinUI 3 lacks a built-in chart, we will use a custom `ItemsControl` or `Grid` with colored `Rectangle`s to represent the waterfall steps.
- Positive values (incomes/margins) in green/blue, negative values (costs) in red/orange.
- The chart should be vertically oriented within the deal card to save horizontal space for more comparisons.

## ViewModel Structure

### ComparisonViewModel
- `ObservableCollection<DealComparisonItemViewModel> ComparedDeals`
- `ICommand AddCurrentDealCommand`
- `ICommand ClearComparisonCommand`

### DealComparisonItemViewModel
- **Inputs**: Snapshot of `MainViewModel` inputs at time of capture.
- **Outputs**: Snapshot of `CalculatorOutputs` and `Profitability`.
- **WaterfallItems**: `ObservableCollection<WaterfallStepViewModel>` for rendering the chart.

### WaterfallStepViewModel
- `string Label`
- `double Value`
- `bool IsTotal` (e.g., for final RoRAC bar)
- `string ColorHex`

## Integration points
- `MainViewModel` will hold an instance of `ComparisonViewModel`.
- "Add to Comparison" button in `MainViewModel` (e.g., in Footer) will trigger adding the current state to `ComparisonViewModel`.

## Next Steps
1. Create `ComparisonViewModel` and related classes.
2. Implement "Add to Comparison" logic in `MainViewModel`.
3. Create `ComparisonView` (UserControl or DataTemplate) for the new tab.
4. Implement custom waterfall chart using standard XAML shapes.