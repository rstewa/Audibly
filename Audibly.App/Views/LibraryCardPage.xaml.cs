// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/16/2024

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.ViewModels;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Sentry;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LibraryCardPage : Page
{
    #region AudioBookFilter enum

    public enum AudioBookFilter
    {
        InProgress,
        NotStarted,
        Completed
    }

    #endregion

    public const string ImportAudiobookText = "Import an audiobook (.m4b, mp3)";

    public const string ImportAudiobooksFromDirectoryText =
        "Import all audiobooks in a directory (recursively). Single-file audiobooks only (.m4b, mp3)";

    public const string ImportAudiobookWithMultipleFilesText =
        "Import an audiobook made up of multiple files (.m4b, mp3)";

    public const string ImportFromJsonFileText = "Import audiobooks from an Audibly export file (.audibly)";
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    private readonly HashSet<AudioBookFilter> _activeFilters = new();

    public LibraryCardPage()
    {
        InitializeComponent();

        // subscribe to page loaded event
        Loaded += LibraryCardPage_Loaded;
        ViewModel.ResetFilters += ViewModelOnResetFilters;
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    private void ViewModelOnResetFilters()
    {
        SelectAllFiltersCheckBox.IsChecked = false;
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

                await _dispatcherQueue.EnqueueAsync(() => ViewModel.IsLoading = true);

                ViewModel.MessageService.ShowDialog(DialogType.Progress, "Data Migration",
                    "Deleting old cover images");

                await ViewModel.AppDataService.DeleteCoverImagesAsync(
                    importedAudiobooks.Select(x => x.CoverImagePath).ToList(),
                    async (i, count, _) =>
                    {
                        await _dispatcherQueue.EnqueueAsync(() =>
                        {
                            ViewModel.ProgressDialogProgress = ((double)i / count * 100).ToInt();
                            ViewModel.ProgressDialogPrefix = "Deleting audiobook";
                            ViewModel.ProgressDialogText = $"{i} of {count}";
                        });
                    });

                await _dispatcherQueue.EnqueueAsync(() =>
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
                await ViewModel.FileImporter.ImportFromJsonAsync(file, new CancellationToken(),
                    async (i, count, title, _) =>
                    {
                        await _dispatcherQueue.EnqueueAsync(() =>
                        {
                            ViewModel.ProgressDialogProgress = ((double)i / count * 100).ToInt();
                            ViewModel.ProgressDialogPrefix = "Importing";
                            ViewModel.ProgressDialogText = $"{title}";
                        });
                    });

                ViewModel.OnProgressDialogCompleted();

                await _dispatcherQueue.EnqueueAsync(() =>
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

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        // unchecked all the filter flyout items
        // InProgressFilterCheckBox.IsChecked =
        //     NotStartedFilterCheckBox.IsChecked = CompletedFilterCheckBox.IsChecked = false;

        await ViewModel.GetAudiobookListAsync();
    }

    /// <summary>
    ///     Resets the audiobook list.
    /// </summary>
    public async Task ResetAudiobookListAsync()
    {
        _activeFilters.Clear();

        // unchecked all the filter flyout items
        InProgressFilterCheckBox.IsChecked = false;
        NotStartedFilterCheckBox.IsChecked = false;
        CompletedFilterCheckBox.IsChecked = false;

        await _dispatcherQueue.EnqueueAsync(() =>
        {
            ViewModel.Audiobooks.Clear();
            foreach (var a in ViewModel.AudiobooksForFilter) ViewModel.Audiobooks.Add(a);
        });
    }

    private HashSet<AudiobookViewModel> GetFilteredAudiobooks()
    {
        // matches audiobooks for each active filter
        var matches = new HashSet<AudiobookViewModel>();

        foreach (var audiobook in ViewModel.AudiobooksForFilter)
        {
            if (_activeFilters.Contains(AudioBookFilter.InProgress) && audiobook.Progress > 0 && !audiobook.IsCompleted)
                matches.Add(audiobook);
            if (_activeFilters.Contains(AudioBookFilter.NotStarted) && audiobook.Progress == 0)
                matches.Add(audiobook);
            if (_activeFilters.Contains(AudioBookFilter.Completed) && audiobook.IsCompleted)
                matches.Add(audiobook);
        }

        return matches;
    }

    /// <summary>
    ///     Filters the audiobook list based on the search text.
    /// </summary>
    private async Task FilterAudiobookList()
    {
        if (_activeFilters.Count == 0)
        {
            await ResetAudiobookListAsync();
            return;
        }

        var matches = GetFilteredAudiobooks();

        await _dispatcherQueue.EnqueueAsync(() =>
        {
            ViewModel.Audiobooks.Clear();
            foreach (var match in matches) ViewModel.Audiobooks.Add(match);
        });
    }

    private void SetCheckedState()
    {
        // Controls are null the first time this is called, so we just 
        // need to perform a null check on any one of the controls.
        if (InProgressFilterCheckBox == null) return;

        // check if any of the filters are checked and change the appbar button background color
        if (InProgressFilterCheckBox.IsChecked == true ||
            NotStartedFilterCheckBox.IsChecked == true ||
            CompletedFilterCheckBox.IsChecked == true)
        {
            FilterButton.BorderBrush = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
            FilterButton.BorderThickness = new Thickness(2);
        }
        else
        {
            FilterButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
            FilterButton.BorderThickness = new Thickness(0);
        }

        if (InProgressFilterCheckBox.IsChecked == true &&
            NotStartedFilterCheckBox.IsChecked == true &&
            CompletedFilterCheckBox.IsChecked == true)
            SelectAllFiltersCheckBox.IsChecked = true;
        else if (InProgressFilterCheckBox.IsChecked == false &&
                 NotStartedFilterCheckBox.IsChecked == false &&
                 CompletedFilterCheckBox.IsChecked == false)
            SelectAllFiltersCheckBox.IsChecked = false;
        else
            // Set third state (indeterminate) by setting IsChecked to null.
            SelectAllFiltersCheckBox.IsChecked = null;
    }

    private async void InProgressFilterCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        SetCheckedState();

        _activeFilters.Add(AudioBookFilter.InProgress);

        await FilterAudiobookList();
    }

    private async void NotStartedFilterCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        SetCheckedState();

        _activeFilters.Add(AudioBookFilter.NotStarted);

        await FilterAudiobookList();
    }

    private async void CompletedFilterCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        SetCheckedState();

        _activeFilters.Add(AudioBookFilter.Completed);

        await FilterAudiobookList();
    }

    private async void InProgressFilterCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        SetCheckedState();

        _activeFilters.Remove(AudioBookFilter.InProgress);

        await FilterAudiobookList();
    }

    private async void NotStartedFilterCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        SetCheckedState();

        _activeFilters.Remove(AudioBookFilter.NotStarted);

        await FilterAudiobookList();
    }

    private async void CompletedFilterCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        SetCheckedState();

        _activeFilters.Remove(AudioBookFilter.Completed);

        await FilterAudiobookList();
    }

    private async void SelectAllFiltersCheckBox_OnChecked(object sender, RoutedEventArgs e)
    {
        InProgressFilterCheckBox.IsChecked =
            NotStartedFilterCheckBox.IsChecked = CompletedFilterCheckBox.IsChecked = true;
    }

    private async void SelectAllFiltersCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
    {
        InProgressFilterCheckBox.IsChecked =
            NotStartedFilterCheckBox.IsChecked = CompletedFilterCheckBox.IsChecked = false;
    }

    private void SelectAllFiltersCheckBox_OnIndeterminate(object sender, RoutedEventArgs e)
    {
        if (InProgressFilterCheckBox.IsChecked == true && NotStartedFilterCheckBox.IsChecked == true &&
            CompletedFilterCheckBox.IsChecked == true)
            SelectAllFiltersCheckBox.IsChecked = false;
    }

    #region debug button

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

    #endregion
}