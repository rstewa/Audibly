using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     Every trophy as a badge, locked ones greyed out.
/// </summary>
public sealed partial class TrophiesPage : Page
{
    public TrophiesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    // an explicit width keeps the panel centered; see StatsPage
    private void Viewport_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ContentPanel.Width = Math.Min(900, e.NewSize.Width);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.ListeningStats.Updated += Reload;
        App.ViewModel.Audiobooks.CollectionChanged += OnLibraryChanged;
        Reload();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        App.ListeningStats.Updated -= Reload;
        App.ViewModel.Audiobooks.CollectionChanged -= OnLibraryChanged;
    }

    private void OnLibraryChanged(object? sender, NotifyCollectionChangedEventArgs e) => Reload();

    private async void Reload()
    {
        var trophies = await App.ListeningStats.LoadTrophiesAsync();
        if (!IsLoaded) return;
        Badges.ItemsSource = trophies;
        SummaryText.Text = string.Create(CultureInfo.InvariantCulture,
            $"{trophies.Count(trophy => trophy.Tier > 0)} of {trophies.Count} unlocked");
    }
}
