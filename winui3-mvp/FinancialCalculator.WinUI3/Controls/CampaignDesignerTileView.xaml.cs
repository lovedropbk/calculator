using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace FinancialCalculator.WinUI3.Controls
{
    public sealed partial class CampaignDesignerTileView : UserControl
    {
        public CampaignDesignerTileView()
        {
            InitializeComponent();
        }

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
                new PropertyMetadata(true));
    }
}