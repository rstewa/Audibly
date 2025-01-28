// Author: rstewa · https://github.com/rstewa
// Updated: 01/28/2025

using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class ZoomLevelUserControl : UserControl
{
    public static readonly DependencyProperty ZoomInButtonIsEnabledProperty = DependencyProperty.Register(
        nameof(ZoomInButtonIsEnabled), typeof(bool), typeof(ZoomLevelUserControl), new PropertyMetadata(default(bool)));

    public static readonly DependencyProperty ZoomOutButtonIsEnabledProperty = DependencyProperty.Register(
        nameof(ZoomOutButtonIsEnabled), typeof(bool), typeof(ZoomLevelUserControl),
        new PropertyMetadata(default(bool)));

    public ZoomLevelUserControl()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    public bool ZoomInButtonIsEnabled
    {
        get => (bool)GetValue(ZoomInButtonIsEnabledProperty);
        set => SetValue(ZoomInButtonIsEnabledProperty, value);
    }

    public bool ZoomOutButtonIsEnabled
    {
        get => (bool)GetValue(ZoomOutButtonIsEnabledProperty);
        set => SetValue(ZoomOutButtonIsEnabledProperty, value);
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.DecreaseAudiobookTileSize();
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IncreaseAudiobookTileSize();
    }
}