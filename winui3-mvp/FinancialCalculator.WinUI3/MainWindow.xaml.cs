using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using FinancialCalculator.WinUI3.ViewModels;
using FinancialCalculator.WinUI3.Services;
using WinRT.Interop;
using System.Collections.Generic;

namespace FinancialCalculator.WinUI3;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        // Important: initialize ViewModel BEFORE InitializeComponent so x:Bind can resolve during load
        ViewModel = new MainViewModel();

        try
        {
            Logger.Info("MainWindow: InitializeComponent start");
            InitializeComponent();
            Logger.Info("MainWindow: InitializeComponent end");
        }
        catch (Microsoft.UI.Xaml.Markup.XamlParseException xpe)
        {
            Logger.Error("XamlParseException in MainWindow.InitializeComponent", xpe);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error("Exception in MainWindow.InitializeComponent", ex);
            throw;
        }

        // Ensure Content DataContext (in case template swapped)
        if (this.Content is FrameworkElement fe)
        {
            fe.DataContext = ViewModel;
        }

        TryApplySystemBackdrop();
        CustomizeTitleBar();
    }

    private void OnStandardCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is CampaignSummaryViewModel row)
            {
                if (ViewModel.CopyToMyCampaignsCommand.CanExecute(row))
                {
                    ViewModel.CopyToMyCampaignsCommand.Execute(row);
                }

                if (ViewModel.CopyToDesignerCommand?.CanExecute(row) ?? false)
                {
                    ViewModel.CopyToDesignerCommand.Execute(row);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Copy click handler failed", ex);
        }
    }

    private async void OnRiskSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var vm = ViewModel.DealInput;
            var prevCustomerType = vm.SelectedCustomerType;
            var prevAssetState = vm.SelectedAssetState;
            var prevAvc = vm.SelectedAssetValuationCurve;
            var prevRating = vm.SelectedRating;

            var labelCol = new ColumnDefinition { Width = (GridLength)Application.Current.Resources["DetailsLabelColumnWidth"] };
            var valueCol = new ColumnDefinition { Width = (GridLength)Application.Current.Resources["DetailsValueColumnWidth"] };
            var contentGrid = new Grid { RowSpacing = (double)Application.Current.Resources["SpaceM"] };
            contentGrid.ColumnDefinitions.Add(labelCol);
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength((double)Application.Current.Resources["SpaceS"]) });
            contentGrid.ColumnDefinitions.Add(valueCol);

            var labelStyle = (Style)Application.Current.Resources["LabelTextStyle"];
            var comboStyle = (Style)Application.Current.Resources["DenseComboBoxStyle"];

            var ctLabel = new TextBlock { Text = "Customer Type", Style = labelStyle };
            Grid.SetRow(ctLabel, 0); Grid.SetColumn(ctLabel, 0);
            var ctCombo = new ComboBox { ItemsSource = vm.CustomerTypes, SelectedItem = vm.SelectedCustomerType, Style = comboStyle, HorizontalAlignment = HorizontalAlignment.Left };
            ctCombo.SelectionChanged += (_, __) => vm.SelectedCustomerType = ctCombo.SelectedItem as string ?? vm.SelectedCustomerType;
            Grid.SetRow(ctCombo, 0); Grid.SetColumn(ctCombo, 2);

            var asLabel = new TextBlock { Text = "Asset State", Style = labelStyle };
            Grid.SetRow(asLabel, 1); Grid.SetColumn(asLabel, 0);
            var asCombo = new ComboBox { ItemsSource = vm.AssetStates, SelectedItem = vm.SelectedAssetState, Style = comboStyle, HorizontalAlignment = HorizontalAlignment.Left };
            asCombo.SelectionChanged += (_, __) => vm.SelectedAssetState = asCombo.SelectedItem as string ?? vm.SelectedAssetState;
            Grid.SetRow(asCombo, 1); Grid.SetColumn(asCombo, 2);

            var avcLabel = new TextBlock { Text = "Asset Class (AVC)", Style = labelStyle };
            Grid.SetRow(avcLabel, 2); Grid.SetColumn(avcLabel, 0);
            var avcCombo = new ComboBox { ItemsSource = vm.AssetValuationCurves, SelectedItem = vm.SelectedAssetValuationCurve, Style = comboStyle, HorizontalAlignment = HorizontalAlignment.Left };
            avcCombo.SelectionChanged += (_, __) => vm.SelectedAssetValuationCurve = avcCombo.SelectedItem as string ?? vm.SelectedAssetValuationCurve;
            Grid.SetRow(avcCombo, 2); Grid.SetColumn(avcCombo, 2);

            var ratingLabel = new TextBlock { Text = "Credit Rating", Style = labelStyle };
            Grid.SetRow(ratingLabel, 3); Grid.SetColumn(ratingLabel, 0);
            var ratingCombo = new ComboBox { ItemsSource = vm.CreditRatings, SelectedItem = vm.SelectedRating, Style = comboStyle, HorizontalAlignment = HorizontalAlignment.Left };
            ratingCombo.SelectionChanged += (_, __) => vm.SelectedRating = ratingCombo.SelectedItem as string ?? vm.SelectedRating;
            Grid.SetRow(ratingCombo, 3); Grid.SetColumn(ratingCombo, 2);

            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            contentGrid.Children.Add(ctLabel);
            contentGrid.Children.Add(ctCombo);
            contentGrid.Children.Add(asLabel);
            contentGrid.Children.Add(asCombo);
            contentGrid.Children.Add(avcLabel);
            contentGrid.Children.Add(avcCombo);
            contentGrid.Children.Add(ratingLabel);
            contentGrid.Children.Add(ratingCombo);

            var dlg = new ContentDialog
            {
                Title = "Risk settings",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = (this.Content as FrameworkElement)?.XamlRoot,
                Content = contentGrid
            };

            var result = await dlg.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                vm.SelectedCustomerType = prevCustomerType;
                vm.SelectedAssetState = prevAssetState;
                vm.SelectedAssetValuationCurve = prevAvc;
                vm.SelectedRating = prevRating;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Risk settings dialog failed", ex);
        }
    }

    private void OnStandardGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        try
        {
            // Find the row VM from the visual tree
            FrameworkElement? fe = e.OriginalSource as FrameworkElement;
            CampaignSummaryViewModel? vm = null;
            while (fe != null && vm == null)
            {
                vm = fe.DataContext as CampaignSummaryViewModel;
                if (vm == null)
                {
                    fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
                }
            }

            // Fallback to currently selected standard campaign
            vm ??= ViewModel.CampaignManager.SelectedCampaign;

            if (vm != null && ViewModel.CopyToMyCampaignsCommand.CanExecute(vm))
            {
                ViewModel.CopyToMyCampaignsCommand.Execute(vm);
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Double-tap copy to My Campaigns failed", ex);
        }
    }


    private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.GoalSeek.IsOpen || ViewModel.GoalSeek.IsAnyTargetSet)
        {
            ViewModel.GoalSeek.CloseCommand.Execute(null);
            args.Handled = true;
        }

    }


    private void TryApplySystemBackdrop()
    {
        // Apply Mica for a modern look; safely ignore if not supported on thisOS
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
            // no-op
        }
    }

    private void CustomizeTitleBar()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                appWindow.Title = "Financial Calculator Pro";
                var titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }
        catch
        {
            // Safe no-op on environments that don't support AppWindow (older Windows)
        }
    }
}
