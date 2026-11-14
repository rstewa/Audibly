using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Audibly.Stats;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace Audibly.App.UserControls;

/// <summary>
///     Listening time per period as bars, with a dashed line at the average. Scrolls sideways when
///     there are more periods than fit and starts at the most recent one.
/// </summary>
public sealed partial class StatsBarChart : UserControl
{
    private const double LabelBandHeight = 28;
    private const double TopPadding = 10;
    private const double MinimumSlotWidth = 56;
    private const double MaximumBarWidth = 48;
    private const double MinimumBarHeight = 2;

    public static readonly DependencyProperty PeriodsProperty = DependencyProperty.Register(nameof(Periods),
        typeof(IReadOnlyList<PeriodTotal>), typeof(StatsBarChart), new PropertyMetadata(null, OnDataChanged));

    public static readonly DependencyProperty AverageSecondsProperty = DependencyProperty.Register(
        nameof(AverageSeconds), typeof(double), typeof(StatsBarChart), new PropertyMetadata(0d, OnDataChanged));

    public StatsBarChart()
    {
        InitializeComponent();
    }

    public IReadOnlyList<PeriodTotal>? Periods
    {
        get => (IReadOnlyList<PeriodTotal>?)GetValue(PeriodsProperty);
        set => SetValue(PeriodsProperty, value);
    }

    public double AverageSeconds
    {
        get => (double)GetValue(AverageSecondsProperty);
        set => SetValue(AverageSecondsProperty, value);
    }

    private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((StatsBarChart)d).Draw(true);

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) => Draw(false);

    private void Draw(bool showLatest)
    {
        Plot.Children.Clear();
        Axis.Children.Clear();
        var periods = Periods ?? [];
        var viewport = PlotScroller.ActualWidth;
        var height = PlotScroller.ActualHeight;
        if (periods.Count == 0 || viewport <= 0 || height <= LabelBandHeight + TopPadding) return;

        var plotHeight = height - LabelBandHeight - TopPadding;
        var width = Math.Max(viewport, periods.Count * MinimumSlotWidth);
        var slot = width / periods.Count;
        var axis = ChartAxis.For(periods.Max(period => period.ListenedSeconds));
        double Y(double seconds) => TopPadding + plotHeight * (1 - Math.Clamp(seconds / axis.MaxSeconds, 0, 1));

        Plot.Width = width;
        Plot.Height = height;

        foreach (var tick in axis.TickSeconds)
        {
            Plot.Children.Add(new Line { X1 = 0, X2 = width, Y1 = Y(tick), Y2 = Y(tick), Style = Style("GridLineStyle") });
            var label = new TextBlock { Text = AxisLabel(tick), Style = Style("ChartLabelStyle") };
            Canvas.SetLeft(label, 10);
            Canvas.SetTop(label, Y(tick) - 9);
            Axis.Children.Add(label);
        }

        var barWidth = Math.Min(MaximumBarWidth, slot * 0.6);
        for (var i = 0; i < periods.Count; i++)
        {
            var period = periods[i];
            var center = slot * (i + 0.5);
            if (period.ListenedSeconds > 0)
            {
                var barHeight = Math.Max(MinimumBarHeight, TopPadding + plotHeight - Y(period.ListenedSeconds));
                var description = $"{period.Label}: {DurationFormat.Short(period.ListenedSeconds)}";
                var bar = new Border { Width = barWidth, Height = barHeight, Style = Style("BarStyle") };
                ToolTipService.SetToolTip(bar, description);
                AutomationProperties.SetName(bar, description);
                Canvas.SetLeft(bar, center - barWidth / 2);
                Canvas.SetTop(bar, TopPadding + plotHeight - barHeight);
                Plot.Children.Add(bar);
            }

            var name = new TextBlock
            {
                Text = period.Label, Width = slot, TextAlignment = TextAlignment.Center, Style = Style("ChartLabelStyle")
            };
            Canvas.SetLeft(name, slot * i);
            Canvas.SetTop(name, TopPadding + plotHeight + 7);
            Plot.Children.Add(name);
        }

        if (AverageSeconds > 0)
            Plot.Children.Add(new Line
            {
                X1 = 0, X2 = width, Y1 = Y(AverageSeconds), Y2 = Y(AverageSeconds), Style = Style("AverageLineStyle"),
                // a dash collection cannot be shared between lines, so it is not part of the style
                StrokeDashArray = [2, 3]
            });

        if (showLatest && width > viewport)
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                // the new canvas width has to be laid out before the view can move to its end
                PlotScroller.UpdateLayout();
                PlotScroller.ChangeView(PlotScroller.ScrollableWidth, null, null, true);
            });
        else if (showLatest)
            PlotScroller.ChangeView(0, null, null, true);
    }

    private Style Style(string key) => (Style)Resources[key];

    private static string AxisLabel(double seconds)
    {
        var minutes = (long)Math.Round(seconds / 60);
        if (minutes == 0) return "0";
        if (minutes % 60 == 0) return (minutes / 60).ToString(CultureInfo.InvariantCulture) + "h";
        return minutes < 60
            ? minutes.ToString(CultureInfo.InvariantCulture) + "m"
            : string.Create(CultureInfo.InvariantCulture, $"{minutes / 60}h {minutes % 60}m");
    }
}
