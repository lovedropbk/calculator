# UI Design: Risk Parameters

**Date:** 2025-10-22
**Goal:** Add user inputs for Basel II risk parameters to the main calculator view.

## 1. Visual Design
A new `Expander` section in `MainWindow.xaml`, likely placed after the main financial inputs and before the campaigns/results, or in a side panel if space permits. Given the current layout, below "Vehicle & Financing" in the left column might be tight.

**Proposed Layout:**
Add to the `Inputs` section in `MainWindow.xaml`.

```xml
<Expander Header="Risk Parameters" IsExpanded="False" Margin="0,12,0,0">
    <Grid RowDefinitions="Auto,Auto,Auto,Auto" ColumnDefinitions="*,*">
        <!-- Customer Type -->
        <TextBlock Text="Customer Type" Grid.Row="0" Grid.Column="0"/>
        <ComboBox SelectedItem="{x:Bind ViewModel.SelectedCustomerType, Mode=TwoWay}" 
                  ItemsSource="{x:Bind ViewModel.CustomerTypes}" 
                  Grid.Row="0" Grid.Column="1"/>

        <!-- Asset State -->
        <TextBlock Text="Asset State" Grid.Row="1" Grid.Column="0"/>
        <ComboBox SelectedItem="{x:Bind ViewModel.SelectedAssetState, Mode=TwoWay}" 
                  ItemsSource="{x:Bind ViewModel.AssetStates}"
                  Grid.Row="1" Grid.Column="1"/>

        <!-- Asset Class -->
        <TextBlock Text="Asset Class (AVC)" Grid.Row="2" Grid.Column="0"/>
        <ComboBox SelectedItem="{x:Bind ViewModel.SelectedAssetValuationCurve, Mode=TwoWay}" 
                  ItemsSource="{x:Bind ViewModel.AssetValuationCurves}"
                  Grid.Row="2" Grid.Column="1"/>

        <!-- Credit Rating -->
        <TextBlock Text="Credit Rating" Grid.Row="3" Grid.Column="0"/>
        <ComboBox SelectedItem="{x:Bind ViewModel.SelectedRating, Mode=TwoWay}" 
                  ItemsSource="{x:Bind ViewModel.CreditRatings}"
                  Grid.Row="3" Grid.Column="1"/>
    </Grid>
</Expander>
```

## 2. ViewModel Changes (`MainViewModel`)
*   **New Properties:**
    *   `SelectedCustomerType` (string)
    *   `SelectedAssetState` (string)
    *   `SelectedAssetValuationCurve` (string)
    *   `SelectedRating` (string)
*   **Collections (static or initialized in ctor):**
    *   `CustomerTypes`: ["RETAIL PRIVATE", "RETAIL SMALL BUSINESS", "FLEET", "DEALER"]
    *   `AssetStates`: ["New", "Used"] (Map to "N"/"U" for engine)
    *   `AssetValuationCurves`: ["MBPC", "MBVA", "OOPC", "MBCV", "FUCV"] (Common ones from CSV)
    *   `CreditRatings`: ["1, 1.0", "2, 2.0", "3, 3.0", "4, 4.0", "5, 5.0", "6, 6.0", "7, 7.0", "8, 8.0"] (Simplified list)

## 3. Integration
Update `MainViewModel.RecalculateAsync` to pass these new properties to `_scenarios.Compute`.