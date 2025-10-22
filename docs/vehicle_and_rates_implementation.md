# Vehicle & Standard Rates Implementation Documentation

**Date:** 2025-10-22
**Status:** Fully Implemented

## Overview

The application now includes advanced auto-population features for vehicles, standard rates, and MBSP packages.

## Key Components

### 1. Vehicle Catalog Service (`VehicleCatalogService.cs`)
- **Data Sources**:
    - `winui3-mvp/docs/RVbymodel OCT2025.csv`: Source for Vehicle Models, MSRP, and Residual Values (RVs).
    - `winui3-mvp/docs/MBSP OCT2025.csv`: Source for MBSP package costs.
- **Functionality**:
    - Loads and merges data from both CSVs.
    - Infers **Vehicle Class** from model names (e.g., "C 220 d" -> "C-Class").
    - Calculates **Class Averages** for MSRP and RVs.
    - Provides a unified list of vehicles (Class Averages + individual Models) for UI selection.

### 2. Standard Rate Service (`StandardRateService.cs`)
- **Data Source**: `winui3-mvp/docs/parameters/standard_rates.csv`
- **Functionality**:
    - Looks up standard customer rates based on:
        - **Product** (HP, mySTAR, etc.)
        - **Term** (months)
        - **Down Payment %**
        - **Payment Mode** (Advance/Arrears)

### 3. UI Integration (`MainViewModel` & `MainWindow.xaml`)
- **Vehicle Selection**:
    - Unified `ComboBox` in the Deal Inputs section allows selecting either a Class Average or a specific Model.
    - Selection auto-populates **Price (MSRP)**.
    - For **mySTAR** product, it auto-populates the **Balloon** value based on the selected term's RV.
    - Displays a warning if the selected vehicle is not eligible for mySTAR (RV is "N/A").
- **Standard Rates**:
    - Automatically updates **Customer Rate** when dependent inputs (Product, Term, Down Payment, Payment Mode) change.
    - Displays a **Deviation Warning** icon next to the rate field if the user-entered rate differs from the standard rate.
- **MBSP Packages (Campaign Designer)**:
    - New `ComboBox` to select **MBSP Package** (e.g., "Easy Care 5").
    - Auto-populates **Free MBSP Cost** based on the selected vehicle and package.
    - Cost field is read-only to ensure accuracy based on catalog.

## File Locations
- **Services**: `winui3-mvp/FinancialCalculator.WinUI3/Services/`
- **Models**: `winui3-mvp/FinancialCalculator.WinUI3/Models/`
- **ViewModels**: `winui3-mvp/FinancialCalculator.WinUI3/ViewModels/`
- **Data Files**: `winui3-mvp/docs/` and `winui3-mvp/docs/parameters/`
