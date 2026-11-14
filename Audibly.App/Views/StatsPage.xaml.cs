using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Audibly.App.ViewModels;
using Audibly.Stats;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     Listening time for the current title and overall, and per day, month and year.
/// </summary>
public sealed partial class StatsPage : Page
{
    private const double StackedCardsBelowWidth = 700;
    private const double MaximumContentWidth = 900;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private StatsData _data = StatsData.Empty;

    public StatsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.ListeningStats.Updated += Reload;
        App.PlayerViewModel.PropertyChanged += OnPlayerChanged;
        App.ViewModel.Audiobooks.CollectionChanged += OnLibraryChanged;
        Reload();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        App.ListeningStats.Updated -= Reload;
        App.PlayerViewModel.PropertyChanged -= OnPlayerChanged;
        App.ViewModel.Audiobooks.CollectionChanged -= OnLibraryChanged;
    }

    private void OnPlayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.NowPlaying)) Show();
    }

    private void OnLibraryChanged(object? sender, NotifyCollectionChangedEventArgs e) => Show();

    private async void Reload()
    {
        _data = await App.ListeningStats.LoadAsync();
        if (IsLoaded) Show();
    }

    private void Show()
    {
        var nowPlaying = App.PlayerViewModel.NowPlaying;
        CurrentTitleText.Text = nowPlaying?.Title ?? "No audiobook open";
        CurrentValues.Visibility = nowPlaying == null ? Visibility.Collapsed : Visibility.Visible;
        if (nowPlaying != null)
        {
            var current = StatsSummary.ForBook(_data, nowPlaying.Id);
            CurrentListenedText.Text = DurationFormat.Short(current.ListenedSeconds);
            CurrentSavedText.Text = DurationFormat.Short(current.SavedSeconds);
        }

        var total = StatsSummary.Total(_data);
        TotalListenedText.Text = DurationFormat.Short(total.ListenedSeconds);
        TotalSavedText.Text = DurationFormat.Short(total.SavedSeconds);
        TotalSinceText.Text = _data.Slices.Count == 0
            ? "Nothing recorded yet"
            : "Since " + _data.Slices.Min(slice => slice.Day).ToString("d MMM yyyy", Invariant);

        var count = App.ViewModel.Audiobooks.Count;
        LibraryCountText.Text = count == 1 ? "1 title" : string.Create(Invariant, $"{count} titles");

        var hasData = total.ListenedSeconds > 0;
        EmptyText.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
        ChartSection.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        if (hasData) ShowPeriods();
    }

    private void ShowPeriods()
    {
        IReadOnlyList<PeriodTotal> periods;
        string eyebrow, firstFormat, lastFormat;
        var selected = PeriodSelector.SelectedItem;
        if (selected == MonthlyItem)
        {
            periods = StatsSummary.Months(_data);
            (eyebrow, firstFormat, lastFormat) = ("MONTHLY AVERAGE", "MMM yyyy", "MMM yyyy");
        }
        else if (selected == YearlyItem)
        {
            periods = StatsSummary.Years(_data);
            (eyebrow, firstFormat, lastFormat) = ("YEARLY AVERAGE", "yyyy", "yyyy");
        }
        else
        {
            periods = StatsSummary.LastListenedDays(_data);
            (eyebrow, firstFormat, lastFormat) = ("DAILY AVERAGE", "d MMM", "d MMM yyyy");
        }

        var average = StatsSummary.Average(periods);
        AverageEyebrowText.Text = eyebrow;
        AverageText.Text = DurationFormat.Short(average);
        RangeText.Text = periods.Count switch
        {
            0 => string.Empty,
            1 => periods[0].Start.ToString(lastFormat, Invariant),
            _ => periods[0].Start.ToString(firstFormat, Invariant) + " – " + periods[^1].Start.ToString(lastFormat, Invariant)
        };
        Chart.AverageSeconds = average;
        Chart.Periods = periods;
    }

    private void PeriodSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (IsLoaded && ChartSection.Visibility == Visibility.Visible) ShowPeriods();
    }

    private void Viewport_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ContentPanel.Width = Math.Min(MaximumContentWidth, e.NewSize.Width);

    private void Content_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var stacked = e.NewSize.Width < StackedCardsBelowWidth;
        Grid.SetColumn(TotalCard, stacked ? 0 : 1);
        Grid.SetRow(TotalCard, stacked ? 1 : 0);
        Grid.SetColumnSpan(CurrentCard, stacked ? 2 : 1);
        Grid.SetColumnSpan(TotalCard, stacked ? 2 : 1);
    }
}
