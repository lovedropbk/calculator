using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using FinancialCalculator.WinUI3.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace FinancialCalculator.WinUI3.Controls;

public sealed partial class WaterfallChart : UserControl
{
    public WaterfallChart()
    {
        this.InitializeComponent();
    }

    public IEnumerable<WaterfallStepViewModel> ItemsSource
    {
        get => (IEnumerable<WaterfallStepViewModel>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<WaterfallStepViewModel>), typeof(WaterfallChart), new PropertyMetadata(null, OnItemsSourceChanged));

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WaterfallChart chart)
        {
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= chart.OnCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += chart.OnCollectionChanged;
            }
            chart.Redraw();
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

    private void Redraw()
    {
        ChartCanvas.Children.Clear();
        if (ItemsSource == null || ChartCanvas.ActualWidth == 0 || ChartCanvas.ActualHeight == 0) return;

        var items = ItemsSource.ToList();
        if (items.Count == 0) return;

        double maxAbsVal = 0;
        double runningTotal = 0;
        foreach(var item in items)
        {
             if (!item.IsTotal) runningTotal += item.Value;
             maxAbsVal = Math.Max(maxAbsVal, Math.Abs(runningTotal));
             maxAbsVal = Math.Max(maxAbsVal, Math.Abs(item.Value)); // Also check individual bar height
        }
        if (maxAbsVal == 0) maxAbsVal = 1; // Prevent div by zero

        // Scale factor to fit in canvas height, leaving space for labels at bottom
        double availableHeight = ChartCanvas.ActualHeight - 30;
        double scaleY = availableHeight / (maxAbsVal * 2.2); // *2 to allow positive and negative from center
        double zeroY = availableHeight / 2 + 10; // Vertically centered zero line

        double barWidth = (ChartCanvas.ActualWidth - 20) / items.Count;
        if (barWidth > 60) barWidth = 60;
        double currentX = 10;
        double currentTotal = 0;

        // Draw Zero Line
        ChartCanvas.Children.Add(new Line
        {
            X1 = 0, Y1 = zeroY,
            X2 = ChartCanvas.ActualWidth, Y2 = zeroY,
            Stroke = ResolveBrush("ControlStrongStrokeColorDefaultBrush"),
            StrokeThickness = 1,
            Opacity = 0.5
        });

        foreach (var item in items)
        {
            double val = item.Value;
            double startY, endY;

            if (item.IsTotal)
            {
                startY = zeroY;
                endY = zeroY - (val * scaleY);
            }
            else
            {
                 startY = zeroY - (currentTotal * scaleY);
                 currentTotal += val;
                 endY = zeroY - (currentTotal * scaleY);
            }

            double rectTop = Math.Min(startY, endY);
            double rectHeight = Math.Abs(endY - startY);
            if (rectHeight < 2) rectHeight = 2; // Min visible height

            var rect = new Rectangle
            {
                Width = Math.Max(1, barWidth - 4),
                Height = rectHeight,
                Fill = ResolveBrush(item.ColorHex),
            };
            Canvas.SetLeft(rect, currentX + 2);
            Canvas.SetTop(rect, rectTop);
            
            ToolTipService.SetToolTip(rect, $"{item.Label}: {item.FormattedValue}");

            ChartCanvas.Children.Add(rect);

            // Connecting lines for waterfall steps
            if (!item.IsTotal && items.IndexOf(item) < items.Count - 1 && !items[items.IndexOf(item)+1].IsTotal)
            {
                 ChartCanvas.Children.Add(new Line
                 {
                     X1 = currentX + barWidth - 2, Y1 = endY,
                     X2 = currentX + barWidth + 6, Y2 = endY,
                     Stroke = ResolveBrush("ControlStrokeColorDefaultBrush"),
                     StrokeThickness = 1,
                     StrokeDashArray = new DoubleCollection { 2, 2 },
                     Opacity = 0.5
                 });
            }

            // Label below bar
            var label = new TextBlock
            {
                Text = item.Label,
                FontSize = 10,
                Width = barWidth,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
                Opacity = 0.8
            };
            Canvas.SetLeft(label, currentX);
            Canvas.SetTop(label, ChartCanvas.ActualHeight - 20);

            ChartCanvas.Children.Add(label);

            currentX += barWidth;
        }
    }

    private Color ParseColor(string hex)
    {
        try
        {
            return (Color)Microsoft.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(Color), hex);
        }
        catch
        {
            return Colors.Gray;
        }
    }

    private SolidColorBrush ResolveBrush(string keyOrHex)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(keyOrHex) && keyOrHex.StartsWith("#"))
            {
                return new SolidColorBrush(ParseColor(keyOrHex));
            }

            // Try theme resources (SolidColorBrush or Color)
            var resources = Application.Current.Resources;
            if (resources.TryGetValue(keyOrHex, out var value))
            {
                if (value is SolidColorBrush sb) return sb;
                if (value is Color c) return new SolidColorBrush(c);
            }
        }
        catch
        {
            // ignore
        }

        return new SolidColorBrush(Colors.Gray);
    }
}