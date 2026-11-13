using System;
using System.ComponentModel;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Audibly.App.UserControls;

/// <summary>
///     Shows the current playback speed as a number. Clicking it opens a flyout with
///     −/+ buttons, a fine-grained slider (0.05× steps) and quick picks.
///     Every speed change reopens the pitch-preserving audio pipeline, so slider and
///     button changes are applied once the user pauses instead of on every step.
/// </summary>
public sealed partial class PlaybackSpeedControl : UserControl
{
    private static readonly TimeSpan ApplyDelay = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan AutoHideDelay = TimeSpan.FromSeconds(2);

    public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register(
        nameof(LabelFontSize), typeof(double), typeof(PlaybackSpeedControl), new PropertyMetadata(14.0));

    private readonly DispatcherQueueTimer _applyTimer;
    private readonly DispatcherQueueTimer _autoHideTimer;
    private double _shownSpeed = 1.0;
    private bool _hasPendingChange;
    private bool _syncingSlider;

    public PlaybackSpeedControl()
    {
        InitializeComponent();

        _applyTimer = DispatcherQueue.CreateTimer();
        _applyTimer.Interval = ApplyDelay;
        _applyTimer.IsRepeating = false;
        _applyTimer.Tick += (_, _) => ApplyPendingSpeed();

        _autoHideTimer = DispatcherQueue.CreateTimer();
        _autoHideTimer.Interval = AutoHideDelay;
        _autoHideTimer.IsRepeating = false;
        _autoHideTimer.Tick += (_, _) => SpeedFlyout.Hide();

        BuildQuickPicks();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public double LabelFontSize
    {
        get => (double)GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    /// <summary>Changes the speed by whole 0.05× steps and briefly shows the flyout (keyboard shortcuts).</summary>
    public void Nudge(int steps)
    {
        ShowBriefly();
        ChangeSpeed(_shownSpeed + steps * PlaybackSpeedPresets.Step, deferApply: true);
    }

    /// <summary>Resets to normal speed and briefly shows the flyout (keyboard shortcut).</summary>
    public void ResetToNormal()
    {
        ShowBriefly();
        ChangeSpeed(1.0, deferApply: false);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.PropertyChanged += PlayerViewModel_PropertyChanged;
        ShowSpeed(PlayerViewModel.PlaybackSpeed);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.PropertyChanged -= PlayerViewModel_PropertyChanged;
        // don't lose a change the user made right before the control went away
        if (_hasPendingChange) ApplyPendingSpeed();
        _autoHideTimer.Stop();
    }

    private void PlayerViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PlayerViewModel.PlaybackSpeed)) return;
        // While the user is still adjusting, their value wins over the player's previous one.
        if (_hasPendingChange) return;
        DispatcherQueue.TryEnqueue(() => { if (!_hasPendingChange) ShowSpeed(PlayerViewModel.PlaybackSpeed); });
    }

    private void BuildQuickPicks()
    {
        var picks = PlaybackSpeedPresets.QuickPicks;
        for (var i = 0; i < picks.Length; i++)
        {
            QuickPickGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var speed = picks[i];
            var button = new HyperlinkButton
            {
                Content = speed.ToString("0.00"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(4, 4, 4, 4),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, $"{speed:0.##} times speed");
            button.Click += (_, _) => ChangeSpeed(speed, deferApply: false);
            Grid.SetColumn(button, i);
            QuickPickGrid.Children.Add(button);
        }
    }

    private void SpeedSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_syncingSlider) return;
        ChangeSpeed(PlaybackSpeedPresets.FromIndex(e.NewValue), deferApply: true, fromSlider: true);
    }

    private void Decrease_Click(object sender, RoutedEventArgs e) =>
        ChangeSpeed(_shownSpeed - PlaybackSpeedPresets.Step, deferApply: true);

    private void Increase_Click(object sender, RoutedEventArgs e) =>
        ChangeSpeed(_shownSpeed + PlaybackSpeedPresets.Step, deferApply: true);

    private void DefaultCheckBox_Click(object sender, RoutedEventArgs e)
    {
        // checked: remember the shown speed for new books; unchecked: fall back to normal speed
        App.ViewModel.DefaultPlaybackSpeed = DefaultCheckBox.IsChecked == true ? _shownSpeed : 1.0;
        UpdateDefaultCheckBox();
    }

    private void SpeedFlyout_Closed(object sender, object e)
    {
        _autoHideTimer.Stop();
        if (_hasPendingChange) ApplyPendingSpeed();
    }

    private void ChangeSpeed(double speed, bool deferApply, bool fromSlider = false)
    {
        speed = PlaybackSpeedPresets.Snap(speed);
        _hasPendingChange = true;
        ShowSpeed(speed, updateSlider: !fromSlider);

        _applyTimer.Stop();
        if (deferApply) _applyTimer.Start();
        else ApplyPendingSpeed();

        if (_autoHideTimer.IsRunning) _autoHideTimer.Start(); // keep a briefly shown flyout open while adjusting
    }

    private void ApplyPendingSpeed()
    {
        _applyTimer.Stop();
        if (!_hasPendingChange) return;
        _hasPendingChange = false;
        if (Math.Abs(PlayerViewModel.PlaybackSpeed - _shownSpeed) > 0.001)
            PlayerViewModel.UpdatePlaybackSpeed(_shownSpeed);
        // the player may have rejected the change; show what is really playing
        ShowSpeed(PlayerViewModel.PlaybackSpeed);
    }

    private void ShowSpeed(double speed, bool updateSlider = true)
    {
        _shownSpeed = PlaybackSpeedPresets.Snap(speed);
        var text = PlaybackSpeedPresets.Format(_shownSpeed);
        SpeedLabel.Text = text;
        FlyoutValueText.Text = text;
        ToolTipService.SetToolTip(SpeedButton, $"Playback speed: {text}");

        if (updateSlider)
        {
            _syncingSlider = true;
            SpeedSlider.Value = PlaybackSpeedPresets.ToIndex(_shownSpeed);
            _syncingSlider = false;
        }

        UpdateDefaultCheckBox();
    }

    private void UpdateDefaultCheckBox() =>
        DefaultCheckBox.IsChecked = Math.Abs(App.ViewModel.DefaultPlaybackSpeed - _shownSpeed) < 0.001;

    private void ShowBriefly()
    {
        if (!SpeedFlyout.IsOpen) SpeedFlyout.ShowAt(SpeedButton);
        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }
}
