// Author: rstewa · https://github.com/rstewa
// Updated: 07/30/2025

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class CustomTimerPopup : UserControl
{
    public CustomTimerPopup()
    {
        InitializeComponent();
    }

    public void Show(XamlRoot xamlRoot)
    {
        TimerPopup.XamlRoot = xamlRoot;
        TimerPopup.IsOpen = true;
    }

    public void ShowAbove(FrameworkElement targetElement, XamlRoot xamlRoot)
    {
        TimerPopup.XamlRoot = xamlRoot;

        // get actual dimensions of the popup and use it to position the popup above the target element

        var transform = targetElement.TransformToVisual(null);
        var point = transform.TransformPoint(new Point(0, 0));

        var popupContent = TimerPopup.Child as FrameworkElement;
        if (popupContent != null)
        {
            popupContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var popupHeight = popupContent.DesiredSize.Height;
            var popupWidth = popupContent.DesiredSize.Width;
            TimerPopup.HorizontalOffset = point.X - popupWidth / 2 - 6;
            TimerPopup.VerticalOffset = point.Y - popupHeight - 16; // 10px spacing
        }
        else
        {
            TimerPopup.HorizontalOffset = point.X;
            TimerPopup.VerticalOffset = point.Y - 10 - 300;
        }

        TimerPopup.IsOpen = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        TimerPopup.IsOpen = false;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var totalSeconds = TimePicker.Time.TotalSeconds;
        if (totalSeconds > 0) App.PlayerViewModel.SetTimer(totalSeconds);
        TimerPopup.IsOpen = false;
    }
}