using System;
using Windows.UI;
using Audibly.Stats;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Audibly.App.UserControls;

/// <summary>
///     One trophy as a round badge. Clicking it opens a dialog with the next goal and the progress.
/// </summary>
public sealed partial class TrophyBadge : UserControl
{
    private static readonly Color Bronze = Color.FromArgb(0xFF, 0xB0, 0x73, 0x3A);
    private static readonly Color Silver = Color.FromArgb(0xFF, 0xA7, 0xB4, 0xC2);
    private static readonly Color Gold = Color.FromArgb(0xFF, 0xD9, 0xA5, 0x20);
    private static bool _dialogOpen;

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State),
        typeof(TrophyState), typeof(TrophyBadge), new PropertyMetadata(null, (d, _) => ((TrophyBadge)d).Show()));

    public TrophyBadge()
    {
        InitializeComponent();
    }

    public TrophyState? State
    {
        get => (TrophyState?)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private static Color TierColor(int tier) => tier switch { 1 => Bronze, 2 => Silver, _ => Gold };

    private void Show()
    {
        if (State is not { } state) return;
        var unlocked = state.Tier > 0;
        LockedRing.Visibility = LockedIcon.Visibility = unlocked ? Visibility.Collapsed : Visibility.Visible;
        UnlockedDisc.Visibility = TierRing.Visibility = UnlockedIcon.Visibility =
            unlocked ? Visibility.Visible : Visibility.Collapsed;
        if (unlocked) TierRing.Stroke = new SolidColorBrush(TierColor(state.Tier));

        LockedIcon.Glyph = UnlockedIcon.Glyph = state.Definition.Glyph;
        NameText.Text = state.Definition.Name;
        NameText.Opacity = unlocked ? 1 : 0.7;
        AutomationProperties.SetName(BadgeButton, $"{state.Definition.Name}, {TrophyCatalog.TierName(state.Tier)}");
        AutomationProperties.SetAutomationId(BadgeButton, "Trophy_" + state.Definition.Id);
    }

    private async void Badge_Click(object sender, RoutedEventArgs e)
    {
        if (State is not { } state || _dialogOpen || XamlRoot == null) return;

        var tier = new TextBlock
        {
            Text = TrophyCatalog.TierName(state.Tier),
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"]
        };
        if (state.Tier > 0) tier.Foreground = new SolidColorBrush(TierColor(state.Tier));
        var goal = new TextBlock { Text = state.GoalText, TextWrapping = TextWrapping.Wrap };
        var progress = new TextBlock { Text = state.ProgressText, TextWrapping = TextWrapping.Wrap };
        var bar = new ProgressBar
        {
            Minimum = 0, Maximum = state.NextThreshold ?? 1,
            Value = state.NextThreshold is { } next ? Math.Min(state.Value, next) : 1,
            Margin = new Thickness(0, 6, 0, 0)
        };
        var content = new StackPanel { Spacing = 10, MinWidth = 320 };
        content.Children.Add(tier);
        content.Children.Add(goal);
        content.Children.Add(progress);
        content.Children.Add(bar);

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = state.Definition.Name,
            Content = content,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = ActualTheme
        };

        _dialogOpen = true;
        try
        {
            await dialog.ShowAsync();
        }
        catch (Exception exception)
        {
            // another dialog of the app is already open
            App.ViewModel.LoggingService.LogError(exception);
        }
        finally
        {
            _dialogOpen = false;
        }
    }
}
