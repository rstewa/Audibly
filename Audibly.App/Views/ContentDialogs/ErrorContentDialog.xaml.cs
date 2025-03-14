using System;
using System.Threading;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Audibly.App.Views.ContentDialogs;

/// <summary>
///     A ContentDialog that displays a progress bar and a message.
/// </summary>
public sealed partial class ErrorContentDialog : ContentDialog
{
    private readonly CancellationTokenSource? _cancellationTokenSource;

    public ErrorContentDialog(CancellationTokenSource? cancellationTokenSource = null)
    {
        InitializeComponent();
        _cancellationTokenSource = cancellationTokenSource;
    }

    public MainViewModel ViewModel => App.ViewModel;

    private void ConfirmCancel_Click(object sender, RoutedEventArgs e)
    {
        // Hide the flyout
        FlyoutBase.GetAttachedFlyout(this).Hide();

        // Close the dialog if "Yes" was clicked
        if ((sender as Button)?.Content.ToString() == "Yes, I really want to cancel.") Hide();
    }

    private void ConfirmCancel_Closed(object sender, object e)
    {
        // Handle flyout closed event if needed
    }

    private async void ErrorContentDialog_OnPrimaryButtonClick(ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        try
        {
            await ViewModel.DeleteAudiobookAsync();
        }
        catch (Exception ex)
        {
            ViewModel.LoggingService.LogError(ex, true);

            // notify user with toast notification
            var notification = new Notification
            {
                Message = $"Failed to delete audiobook: {ex.Message}",
                Severity = InfoBarSeverity.Error
            };
            ViewModel.EnqueueNotification(notification);
        }
        finally
        {
            await ViewModel.GetAudiobookListAsync();
        }
    }
}