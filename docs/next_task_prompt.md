You are tasked with implementing advanced auto-population features for the Financial Calculator.

## Objectives
1.  **Vehicle Selection UI**:
    *   Add a vehicle selection mechanism to the main calculator UI (likely above Price).
    *   Allow filtering/selection by **Vehicle Class** first (e.g., C-Class, E-Class), then by specific **Model Name**.
    *   **Class Averages**: Include an option to select the "Average" for a class, which should auto-populate the average MSRP and average RVs for that class. (Note: You may need to calculate these averages dynamically from `vehicle_catalog.csv` or pre-calculate them).
    *   Use `winui3-mvp/docs/parameters/vehicle_catalog.csv` as the data source.
    *   When a model (or class average) is selected, auto-populate the **Price** (MSRP) field.
    *   If a Residual Value (balloon) product is selected (e.g., mySTAR), auto-populate the **Balloon** value based on the selected term (RV12, RV24, etc. columns in the catalog).
    *   **Eligibility**: If the selected vehicle has "N/A" for RVs, it is **NOT eligible** for mySTAR product. Display a warning or prevent selection.

2.  **Standard Rate Auto-Population**:
    *   When users change Product, Term, Downpayment, or Payment Mode, auto-populate the **Customer Rate** based on standard pricing.
    *   Use `winui3-mvp/docs/parameters/standard_rates.csv` as the data source.
    *   **Matching Rules**:
        *   Match by Product (HP, mySTAR, FL), Term, Downpayment % range (`DPMin` <= dp < `DPMax`), and Payment Mode (Advance/Arrears).
        *   `mySTAR` and `FL` rates might have `Any` as Payment Mode, meaning they apply to both.
    *   If current selections don't match a standard rate, handle gracefully.

3.  **Deviation Warning**:
    *   Allow users to manually override the auto-populated Customer Rate.
    *   If the entered rate deviates from the standard rate for the current parameters, display a visual warning (e.g., yellow highlight, warning icon with tooltip) to inform the user they are off-standard.

## Implementation Guidance
*   Start in **Architect Mode** to plan the UI changes and services.
*   Create new services (e.g., `VehicleCatalogService`, `StandardRateService`) to handle data loading and lookups.
*   Ensure `MainViewModel` handles dependencies (e.g., changing Term updates standard rate AND RV).

## Learnings from Previous Phase
*   **Tool Use**: When using `apply_diff`, ensure the `SEARCH` block matches the file content *exactly*, character-for-character, including whitespace. If `apply_diff` fails repeatedly due to mismatch or apparent content corruption in the tool call, prefer using `write_to_file` to overwrite the entire file with the known correct content, especially for smaller files.
*   **CSV Parsing**: Simple `string.Split(',')` can fail if fields contain commas (e.g., quoted strings). Use a robust CSV parser or the provided `SplitCsvLine` helper if applicable.