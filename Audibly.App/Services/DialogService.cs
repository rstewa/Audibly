// Author: rstewa · https://github.com/rstewa
// Created: 12/17/2024
// Updated: 01/01/2025

using System.Threading;
using System.Threading.Tasks;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.ViewModels;
using Audibly.App.Views.ContentDialogs;
using Audibly.App.Views.ControlPages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Services;

public static class DialogService
{
    private static readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private static ContentDialog? _progressDialog;

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public static MainViewModel ViewModel => App.ViewModel;

    internal static async Task ShowErrorDialogAsync(string title, string content)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                XamlRoot = App.Window.Content.XamlRoot
            }.SetPrimaryButton("OK");
            await errorDialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task ShowOkDialogAsync(string title, string content)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                RequestedTheme = ThemeHelper.ActualTheme,
                XamlRoot = App.Window.Content.XamlRoot
            }.SetPrimaryButton("OK");
            await dialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task ShowOnboardingDialogAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            // show onboarding dialog
            var dialog = new ContentDialog
            {
                Title = "Welcome to Audibly!",
                Content = "We're glad you're here. Let's get started by adding your first audiobook.",
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = ThemeHelper.ActualTheme,
                XamlRoot = App.Window.Content.XamlRoot
            };
            await dialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task ShowChangelogDialogAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            // show changelog dialog
            var dialog = new ChangelogContentDialog
            {
                XamlRoot = App.Window.Content.XamlRoot,
                RequestedTheme = ThemeHelper.ActualTheme
            };

            await dialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task<ContentDialogResult> ShowSelectFilesDialogAsync()
    {
        var result = ContentDialogResult.None;
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var selectFilesDialog = new SelectFilesDialog();

            var contentDialog = new ContentDialog
            {
                Title = "Put Selected Files in Order (Drag and Drop)",
                Content = selectFilesDialog,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = App.Window.Content.XamlRoot,
                MinWidth = selectFilesDialog.ActualWidth
            };

            result = await contentDialog.ShowOneAtATimeAsync();
        });

        return result;
    }

    internal static async Task ShowDataMigrationRequiredDialogAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "Data Migration Required",
                Content =
                    "To ensure compatibility with the latest update, we need to migrate your data to the new database " +
                    "format. This process may take a few minutes depending on the size of your library. Do not close the app " +
                    "during this process.",
                DefaultButton = ContentDialogButton.Primary,
                PrimaryButtonText = "Migrate Data",
                XamlRoot = App.Window.Content.XamlRoot,
                RequestedTheme = ThemeHelper.ActualTheme
            }.SetPrimaryButton("Migrate Data", async (_, _) => await ViewModel.MigrateDatabase());
            await dialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task ShowDataMigrationFailedDialogAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "Data Migration Failed",
                Content =
                    "We were unable to migrate your data from the previous version of Audibly. Please contact support for assistance.",
                CloseButtonText = "Ok",
                DefaultButton = ContentDialogButton.Close,
                RequestedTheme = ThemeHelper.ActualTheme,
                XamlRoot = App.Window.Content.XamlRoot
            };
            await dialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task ShowMoreInfoDialogAsync(AudiobookViewModel audiobookViewModel)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            var moreInfoDialog = new MoreInfoDialogContent(audiobookViewModel);

            var contentDialog = new ContentDialog
            {
                Title = "More Info",
                Content = moreInfoDialog,
                CloseButtonText = "Close",
                XamlRoot = App.Window.Content.XamlRoot,
                RequestedTheme = ThemeHelper.ActualTheme,
                MinWidth = moreInfoDialog.ActualWidth
            };
            
            // todo: decide if I want to use this
            // contentDialog.Background = (Brush)Application.Current.Resources["AcrylicBackgroundFillColorBaseBrush"];
            
            await contentDialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task ShowProgressDialogAsync(string title, CancellationTokenSource? cts,
        bool showCancelButton = true)
    {
        // yes, I'm intentionally not awaiting this
        _dispatcherQueue.EnqueueAsync(async () =>
        {
            _progressDialog = new ProgressContentDialog(cts)
            {
                Title = title,
                XamlRoot = App.Window.Content.XamlRoot
            };

            if (showCancelButton)
            {
                _progressDialog.DefaultButton = ContentDialogButton.Close;
                _progressDialog.SetCloseButton("Cancel");
            }

            // todo: should i pass cts to ShowOneAtATimeAsync?
            await _progressDialog.ShowOneAtATimeAsync();
        });
    }

    internal static async Task CloseProgressDialogAsync()
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            if (_progressDialog == null) return;
            _progressDialog.Hide();
            _progressDialog = null;
        });
    }
}