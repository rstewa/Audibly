// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/16/2024

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Sentry;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryCardPage : Page
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public const string ImportAudiobookText = "Import an audiobook (.m4b, mp3)";

    public const string ImportAudiobooksFromDirectoryText =
        "Import all audiobooks in a directory (recursively). Single-file audiobooks only (.m4b, mp3)";

    public const string ImportAudiobookWithMultipleFilesText =
        "Import an audiobook made up of multiple files (.m4b, mp3)";

    public const string ImportFromJsonFileText = "Import audiobooks from an Audibly export file (.audibly)";

    public LibraryCardPage()
    {
        InitializeComponent();

        // subscribe to page loaded event
        Loaded += LibraryCardPage_Loaded;
    }

    private async void LibraryCardPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.NeedToImportAudiblyExport) return;

        var element = (FrameworkElement)sender;
        // let the user know that we need to migrate their data into the new database
        // todo: re-word this message && change the width of the dialog
        // todo: add try-catch block
        await element.ShowConfirmDialogAsync("Data Migration Required",
            "To ensure compatibility with the latest update, we need to migrate your data to the new database format. This process may take a few minutes depending on the size of your library.",
            async () =>
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync("audibly_export.audibly");

                if (file == null) return;

                var json = await FileIO.ReadTextAsync(file);
                var importedAudiobooks = JsonSerializer.Deserialize<List<ImportedAudiobook>>(json);

                if (importedAudiobooks == null)
                {
                    // log the error
                    App.ViewModel.LoggingService.LogError(new Exception("Failed to deserialize the json file"));
                    return;
                }

                // delete the old cover images

                await dispatcherQueue.EnqueueAsync(() => ViewModel.IsLoading = true);

                ViewModel.MessageService.ShowDialog(DialogType.Progress, "Data Migration",
                    "Deleting old cover images");

                await ViewModel.AppDataService.DeleteCoverImagesAsync(
                    importedAudiobooks.Select(x => x.CoverImagePath).ToList(),
                    async (i, count, _) =>
                    {
                        await dispatcherQueue.EnqueueAsync(() =>
                        {
                            ViewModel.ProgressDialogProgress = ((double)i / count * 100).ToInt();
                            ViewModel.ProgressDialogPrefix = "Deleting audiobook";
                            ViewModel.ProgressDialogText = $"{i} of {count}";
                        });
                    });

                await dispatcherQueue.EnqueueAsync(() =>
                {
                    // ViewModel.ProgressDialogText = string.Empty;
                    ViewModel.ProgressDialogProgress = 0;
                });

                // notify user that we successfully deleted the old cover images
                ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Deleted audiobooks from old database",
                    Severity = InfoBarSeverity.Success
                });

                // re-import the user's audiobooks
                await ViewModel.FileImporter.ImportFromJsonAsync(file, new System.Threading.CancellationToken(),
                    async (i, count, title, _) =>
                    {
                        await dispatcherQueue.EnqueueAsync(() =>
                        {
                            ViewModel.ProgressDialogProgress = ((double)i / count * 100).ToInt();
                            ViewModel.ProgressDialogPrefix = "Importing";
                            ViewModel.ProgressDialogText = $"{title}";
                        });
                    });

                ViewModel.OnProgressDialogCompleted();

                await dispatcherQueue.EnqueueAsync(() =>
                {
                    ViewModel.ProgressDialogPrefix = string.Empty;
                    ViewModel.ProgressDialogText = string.Empty;
                    ViewModel.ProgressDialogProgress = 0;
                    ViewModel.IsLoading = false;
                });

                // notify user that we successfully imported their audiobooks
                ViewModel.EnqueueNotification(new Notification
                {
                    Message = "Data migration completed successfully",
                    Severity = InfoBarSeverity.Success
                });

                ViewModel.NeedToImportAudiblyExport = false;

                await ViewModel.GetAudiobookListAsync(true);
            });
    }

    private void TestContentDialogButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.MessageService.ShowDialog(DialogType.Changelog, "What's New?", Changelog.Text);
    }

    private void InfoBar_OnClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        // get the notification object
        if (sender.DataContext is not Notification notification) return;
        ViewModel.OnNotificationClosed(notification);
    }

    private void TestNotificationButton_OnClick(object sender, RoutedEventArgs e)
    {
        // randomly select InfoBarSeverity
        var random = new Random();
        var severity = random.Next(0, 4);

        ViewModel.EnqueueNotification(new Notification
        {
            Message = "This is a test notification",
            Severity = severity switch
            {
                0 => InfoBarSeverity.Informational,
                1 => InfoBarSeverity.Success,
                2 => InfoBarSeverity.Warning,
                3 => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational
            }
        });
    }

    public void ThrowExceptionButton_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("This is a test exception");
    }

    public void RestartAppButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.RestartApp();
    }

    public void HideNowPlayingBarButton_OnClick(object sender, RoutedEventArgs e)
    {
        PlayerViewModel.MediaPlayer.Pause();
        if (PlayerViewModel.NowPlaying != null)
            PlayerViewModel.NowPlaying.IsNowPlaying = false;
        PlayerViewModel.NowPlaying = null;
    }

    public void OpenAppStateFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var filePath = ApplicationData.Current.LocalFolder.Path;
        Process p = new();
        p.StartInfo.FileName = "explorer.exe";
        p.StartInfo.Arguments = $"/open, \"{filePath}\"";
        p.Start();
    }

    private void DebugMenuKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender,
        KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel.ShowDebugMenu = !ViewModel.ShowDebugMenu;
    }

    private void OpenCurrentAudiobooksAppStateFolder_OnClick(object sender, RoutedEventArgs e)
    {
        var selectedAudiobook = PlayerViewModel.NowPlaying;
        if (selectedAudiobook == null) return;
        var dir = Path.GetDirectoryName(selectedAudiobook.CoverImagePath);
        if (dir == null) return;
        Process p = new();
        p.StartInfo.FileName = "explorer.exe";
        p.StartInfo.Arguments = $"/open, \"{dir}\"";
        p.Start();
    }

    private void TestSentryLoggingButton_OnClick(object sender, RoutedEventArgs e)
    {
        SentrySdk.CaptureMessage("Something went wrong");
        ViewModel.EnqueueNotification(new Notification
        {
            Message = "Sentry message sent",
            Severity = InfoBarSeverity.Success
        });
    }
}