using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Windows.Input;
using FinancialCalculator.WinUI3.ViewModels;

namespace FinancialCalculator.WinUI3.Controls
{
    public sealed partial class CampaignDesignerTilesView : UserControl
    {
        public CampaignDesignerTilesView()
        {
            InitializeComponent();
            // Provide a default remove command if one isn't supplied by the parent
            if (RemoveTileCommand == null)
            {
                RemoveTileCommand = new SimpleCommand(async (obj) => await OnRemoveTileRequestedAsync(obj));
            }

            // Ensure initial layout strategy is applied after the visual tree is available
            Loaded += (_, __) => UpdateLayoutStrategy();
        }

        // Layout strategy enum to switch between ListView+ItemsWrapGrid and ItemsRepeater+UniformGridLayout
        public enum TilesLayoutStrategy
        {
            ListViewWrap = 0,
            ItemsRepeaterUniformGrid = 1
        }

        public TilesLayoutStrategy LayoutStrategy
        {
            get => (TilesLayoutStrategy)GetValue(LayoutStrategyProperty);
            set => SetValue(LayoutStrategyProperty, value);
        }

        public static readonly DependencyProperty LayoutStrategyProperty =
            DependencyProperty.Register(
                nameof(LayoutStrategy),
                typeof(TilesLayoutStrategy),
                typeof(CampaignDesignerTilesView),
                new PropertyMetadata(TilesLayoutStrategy.ListViewWrap, OnLayoutStrategyChanged));

        private static void OnLayoutStrategyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CampaignDesignerTilesView view)
            {
                view.UpdateLayoutStrategy();
            }
        }

        private void UpdateLayoutStrategy()
        {
            if (TilesList is null || TilesRepeater is null) return;

            if (LayoutStrategy == TilesLayoutStrategy.ListViewWrap)
            {
                TilesList.Visibility = Visibility.Visible;
                TilesList.IsHitTestVisible = true;
                TilesRepeater.Visibility = Visibility.Collapsed;
                TilesRepeater.IsHitTestVisible = false;
            }
            else
            {
                TilesList.Visibility = Visibility.Collapsed;
                TilesList.IsHitTestVisible = false;
                TilesRepeater.Visibility = Visibility.Visible;
                TilesRepeater.IsHitTestVisible = true;
            }
        }

        // Expose a global detail flag that parents can bind to (e.g., Comparison.IsDesignerDetailed)
        public bool IsDetailed
        {
            get => (bool)GetValue(IsDetailedProperty);
            set => SetValue(IsDetailedProperty, value);
        }

        public static readonly DependencyProperty IsDetailedProperty =
            DependencyProperty.Register(
                nameof(IsDetailed),
                typeof(bool),
                typeof(CampaignDesignerTilesView),
                new PropertyMetadata(true));

        // Parent-supplied remove command (optional). Defaults to a confirmation dialog + remove.
        public ICommand? RemoveTileCommand
        {
            get => (ICommand?)GetValue(RemoveTileCommandProperty);
            set => SetValue(RemoveTileCommandProperty, value);
        }

        public static readonly DependencyProperty RemoveTileCommandProperty =
            DependencyProperty.Register(
                nameof(RemoveTileCommand),
                typeof(ICommand),
                typeof(CampaignDesignerTilesView),
                new PropertyMetadata(null));

        private async System.Threading.Tasks.Task OnRemoveTileRequestedAsync(object? parameter)
        {
            try
            {
                if (parameter is not CampaignTileViewModel tile) return;

                var dialog = new ContentDialog
                {
                    Title = "Delete campaign?",
                    Content = $"Are you sure you want to delete '{tile.Title}'?",
                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.Comparison.DesignerCampaigns.Remove(tile);
                    }
                }
            }
            catch
            {
                // best-effort remove; no user-visible error to keep flow smooth
            }
        }

        // Minimal ICommand implementation for inline commands
        private sealed class SimpleCommand : ICommand
        {
            private readonly Func<object?, System.Threading.Tasks.Task>? _asyncExecute;
            private readonly Action<object?>? _execute;
            private readonly Func<object?, bool>? _canExecute;

            public SimpleCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public SimpleCommand(Func<object?, System.Threading.Tasks.Task> asyncExecute, Func<object?, bool>? canExecute = null)
            {
                _asyncExecute = asyncExecute;
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;
            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public async void Execute(object? parameter)
            {
                if (!CanExecute(parameter)) return;

                if (_asyncExecute != null)
                {
                    await _asyncExecute(parameter);
                }
                else
                {
                    _execute?.Invoke(parameter);
                }
            }

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}