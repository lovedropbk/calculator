using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Dispatching;
using FinancialCalculator.WinUI3.ViewModels;
using FinancialCalculator.WinUI3.Services;
using Microsoft.UI.Xaml.Controls.Primitives;
using WinRT.Interop;
using System.Linq;

namespace FinancialCalculator.WinUI3;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    private readonly AppSettingsService _settings = new();
    private readonly LayoutCoordinator _layout = new();

    public MainWindow()
    {
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

        if (Content is FrameworkElement fe)
        {
            fe.DataContext = ViewModel;
        }

        TryApplySystemBackdrop();
        CustomizeTitleBar();

        try
        {
            _settings.Load();
            if (Content is FrameworkElement root)
            {
                _settings.ApplyTheme(root);
            }
            _layout.Attach(ViewModel, _settings);

            // React to theme changes from SettingsViewModel to apply immediately to the window
            try
            {
                ViewModel.Settings.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ViewModels.Settings.SettingsViewModel.Theme))
                    {
                        if (Content is FrameworkElement root)
                        {
                            ViewModel.Settings.ApplyTheme(root);
                        }
                    }
                };
            }
            catch { }
        }
        catch { }
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

    // Settings are now bound to ViewModel.Settings. This handler remains as no-op for back-compat.
    private void OnAutoExpandToggleClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is ToggleMenuFlyoutItem t)
            {
                _settings.AutoExpandRightOnLeftCollapse = t.IsChecked;
                _settings.Save();
            }
        }
        catch { }
    }

    // No-op, data bound to ViewModel.Settings
    private void OnThemeDefaultClicked(object sender, RoutedEventArgs e) { }
    // No-op, data bound to ViewModel.Settings
    private void OnThemeLightClicked(object sender, RoutedEventArgs e) { }
    // No-op, data bound to ViewModel.Settings
    private void OnThemeDarkClicked(object sender, RoutedEventArgs e) { }

    // No-op, data bound to ViewModel.Settings
    private void OnDensityCompactClicked(object sender, RoutedEventArgs e) { }
    // No-op, data bound to ViewModel.Settings
    private void OnDensityComfortableClicked(object sender, RoutedEventArgs e) { }

    // No-op, theme is applied via AppSettingsService and SettingsViewModel
    private void ApplyTheme(ElementTheme theme) { }

    private void OnStandardGridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        try
        {
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

    private void OnLeftCollapseToggleClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (ViewModel.DealInput.IsDealInputsCollapsed && _settings.AutoExpandRightOnLeftCollapse)
                {
                    ViewModel.IsCampaignDetailsCollapsed = false;
                }
            });
        }
        catch { }
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
        try
        {
            SystemBackdrop = new MicaBackdrop();
        }
        catch
        {
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
        }
    }

    private void OnAppSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.Flyout is FlyoutBase fb)
            {
                fb.ShowAt(btn);
            }
        }
        catch { }
    }

    
}
