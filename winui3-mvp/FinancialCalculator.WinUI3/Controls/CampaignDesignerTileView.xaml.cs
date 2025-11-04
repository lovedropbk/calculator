using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace FinancialCalculator.WinUI3.Controls
{
    public sealed partial class CampaignDesignerTileView : UserControl
    {
        private bool _isUpdatingIsDetailed;

        public CampaignDesignerTileView()
        {
            InitializeComponent();
            UpdateEffectiveIsDetailed();
        }

        // Parent-supplied remove command (optional)
        public ICommand? RemoveCommand
        {
            get => (ICommand?)GetValue(RemoveCommandProperty);
            set => SetValue(RemoveCommandProperty, value);
        }

        public static readonly DependencyProperty RemoveCommandProperty =
            DependencyProperty.Register(
                nameof(RemoveCommand),
                typeof(ICommand),
                typeof(CampaignDesignerTileView),
                new PropertyMetadata(null));

        // Tri-state mode: Global (inherits), Compact (force false), Detailed (force true)
        public enum DetailMode
        {
            Global = 0,
            Compact = 1,
            Detailed = 2
        }

        public DetailMode TileDetailMode
        {
            get => (DetailMode)GetValue(TileDetailModeProperty);
            set => SetValue(TileDetailModeProperty, value);
        }

        public static readonly DependencyProperty TileDetailModeProperty =
            DependencyProperty.Register(
                nameof(TileDetailMode),
                typeof(DetailMode),
                typeof(CampaignDesignerTileView),
                new PropertyMetadata(DetailMode.Global, OnTileDetailModeChanged));

        private static void OnTileDetailModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (CampaignDesignerTileView)d;
            view.UpdateEffectiveIsDetailed();
        }

        // Global detail state from parent container (previously bound to IsDetailed directly)
        public bool GlobalIsDetailed
        {
            get => (bool)GetValue(GlobalIsDetailedProperty);
            set => SetValue(GlobalIsDetailedProperty, value);
        }

        public static readonly DependencyProperty GlobalIsDetailedProperty =
            DependencyProperty.Register(
                nameof(GlobalIsDetailed),
                typeof(bool),
                typeof(CampaignDesignerTileView),
                new PropertyMetadata(true, OnGlobalIsDetailedChanged));

        private static void OnGlobalIsDetailedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (CampaignDesignerTileView)d;
            if (view.TileDetailMode == DetailMode.Global)
            {
                view.UpdateEffectiveIsDetailed();
            }
        }

        // Effective detail state used by the UI and existing bindings
        public bool IsDetailed
        {
            get => (bool)GetValue(IsDetailedProperty);
            set => SetValue(IsDetailedProperty, value);
        }

        public static readonly DependencyProperty IsDetailedProperty =
            DependencyProperty.Register(
                nameof(IsDetailed),
                typeof(bool),
                typeof(CampaignDesignerTileView),
                new PropertyMetadata(true, OnIsDetailedChanged));

        private static void OnIsDetailedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (CampaignDesignerTileView)d;
            if (view._isUpdatingIsDetailed) return;

            // User toggled the switch; convert to local override if currently inheriting
            bool newVal = (bool)e.NewValue;
            if (view.TileDetailMode == DetailMode.Global)
            {
                view.TileDetailMode = newVal ? DetailMode.Detailed : DetailMode.Compact;
            }
        }

        private void UpdateEffectiveIsDetailed()
        {
            _isUpdatingIsDetailed = true;
            try
            {
                bool effective = TileDetailMode switch
                {
                    DetailMode.Global => GlobalIsDetailed,
                    DetailMode.Detailed => true,
                    DetailMode.Compact => false,
                    _ => GlobalIsDetailed
                };
                SetValue(IsDetailedProperty, effective);
            }
            finally
            {
                _isUpdatingIsDetailed = false;
            }
        }

        // UI handler for the "Reset to global" mini button
        private void OnResetToGlobalClicked(object sender, RoutedEventArgs e)
        {
            TileDetailMode = DetailMode.Global;
            UpdateEffectiveIsDetailed();
        }
    }
}