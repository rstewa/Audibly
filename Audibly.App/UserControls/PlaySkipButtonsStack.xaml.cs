// Author: rstewa · https://github.com/rstewa
// Updated: 02/14/2025

using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class PlaySkipButtonsStack : UserControl
{
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(PlaySkipButtonsStack), new PropertyMetadata(0.0));

    public static readonly DependencyProperty PlayButtonSizeProperty = DependencyProperty.Register(
        nameof(PlayButtonSize), typeof(double), typeof(PlaySkipButtonsStack), new PropertyMetadata(32.0));

    public PlaySkipButtonsStack()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UserSettings.SkipAmountsChanged -= UpdateSkipAmountLabels;
            UserSettings.SkipAmountsChanged += UpdateSkipAmountLabels;
            UpdateSkipAmountLabels();
        };
        Unloaded += (_, _) => UserSettings.SkipAmountsChanged -= UpdateSkipAmountLabels;
    }

    private void UpdateSkipAmountLabels()
    {
        SkipBackLabel.Text = UserSettings.SkipBackSeconds.ToString("0");
        SkipForwardLabel.Text = UserSettings.SkipForwardSeconds.ToString("0");
        ToolTipService.SetToolTip(SkipBackButton, $"Skip backward {UserSettings.SkipBackSeconds:0} seconds");
        ToolTipService.SetToolTip(SkipForwardButton, $"Skip forward {UserSettings.SkipForwardSeconds:0} seconds");
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public double PlayButtonSize
    {
        get => (double)GetValue(PlayButtonSizeProperty);
        set => SetValue(PlayButtonSizeProperty, value);
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (PlayerViewModel.PlayPauseIcon == Symbol.Play)
            PlayerViewModel.MediaPlayer.Play();
        else
            PlayerViewModel.MediaPlayer.Pause();
    }

    private async void PreviousChapterButton_Click(object sender, RoutedEventArgs e)
    {
        await PlayerViewModel.PreviousChapter();
    }

    private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
    {
        await PlayerViewModel.NextChapter();
    }

    private async void SkipBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PlayerViewModel.SkipBack();
    }

    private async void SkipForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        await PlayerViewModel.SkipForward();
    }
}