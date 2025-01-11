// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/14/2024

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.Services.Interfaces;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Sentry;
using Sharpener.Extensions;
using WinRT.Interop;

namespace Audibly.App.ViewModels;

/// <summary>
///     Provides data and commands accessible to the entire app.
/// </summary>
public class MainViewModel : BindableBase
{
    #region Delegates

    public delegate void ResetFiltersHandler();

    #endregion

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public readonly IAppDataService AppDataService;
    public readonly IImportFiles FileImporter;
    public readonly IloggingService LoggingService;

    private CancellationTokenSource? _cancellationTokenSource;

    private bool _isImporting;

    private bool _isLoading;

    private bool _isNavigationViewVisible = true;

    // todo: don't need this anymore
    private bool _isNotificationBarVisible;
    
    private const double _maximumTitleFontSize = 34.2;
    private const double _maximumAuthorFontSize = 26.59;
    private const double _maximumTitleMaxWidth = 380;
    private const double _maximumPlayButtonHeightWidth = 133;
    private const double _maximumProgressIndicatorTextFontSize = 38;
    private const double _maximumProgressIndicatorFontSize = 76;
    private const double _maximumAudiobookTileWidth = 570;
    
    private const double _minimumTitleFontSize = 10.79;
    private const double _minimumAuthorFontSize = 8.39;
    private const double _minimumTitleMaxWidth = 120;
    private const double _minimumPlayButtonHeightWidth = 42;
    private const double _minimumProgressIndicatorTextFontSize = 12;
    private const double _minimumProgressIndicatorFontSize = 24;
    private const double _minimumAudiobookTileWidth = 180;

    private InfoBarSeverity _notificationSeverity;

    private string _notificationText = string.Empty;

    private string _progressDialogPrefix = string.Empty;

    private int _progressDialogProgress;

    private string _progressDialogText = string.Empty;

    private string _progressDialogTotalText = string.Empty;
    
    private readonly double _audiobookTileWidthIncrement = 30;
    private readonly double _authorFontSizeIncrement = 1.4;
    private readonly double _playButtonHeightWidthIncrement = 7;
    private readonly double _progressIndicatorFontSizeIncrement = 4;
    private readonly double _progressIndicatorTextFontSizeIncrement = 2;
    private readonly double _titleFontSizeIncrement = 1.8;
    private readonly double _titleMaxWidthIncrement = 20;

    private AudiobookViewModel? _selectedAudiobook;

    private bool _showDebugMenu;

    private bool _showStartPanel;


    /// <summary>
    ///     Creates a new MainViewModel.
    /// </summary>
    public MainViewModel(IImportFiles fileImporter, IAppDataService appDataService, IloggingService loggingService)
    {
        FileImporter = fileImporter;
        AppDataService = appDataService;
        LoggingService = loggingService;
        Task.Run(() => GetAudiobookListAsync(true));

        // todo: this is temporary
        // TitleFontSize = 18;
        // AuthorFontSize = 14;
        // TitleMaxWidth = 200;
        // PlayButtonHeightWidth = 70;
        // ProgressIndicatorTextFontSize = 20; // todo
        // ProgressIndicatorFontSize = 40;
        // AudiobookTileWidth = 300;
        // AudiobookTileMinColumnSpacing = 28;
        
        TitleFontSize = 18; // 1.8
        AuthorFontSize = 14; // 1.4
        TitleMaxWidth = 200; // 20
        PlayButtonHeightWidth = 70; // 7
        ProgressIndicatorTextFontSize = 20; // 2
        ProgressIndicatorFontSize = 40; // 4
        AudiobookTileWidth = 300; // 30
        AudiobookTileMinColumnSpacing = 28; // 2.8
    }

    public string FileActivationError { get; set; } = string.Empty;

    /// <summary>
    ///     The collection of audiobooks in the list.
    /// </summary>
    public ObservableCollection<AudiobookViewModel> Audiobooks { get; } = [];

    /// <summary>
    ///     The collection of audiobooks to be used for filtering.
    /// </summary>
    public List<AudiobookViewModel> AudiobooksForFilter { get; } = [];

    /// <summary>
    ///     Gets or sets the selected audiobook, or null if no audiobook is selected.
    /// </summary>
    public AudiobookViewModel? SelectedAudiobook
    {
        get => _selectedAudiobook;
        set => Set(ref _selectedAudiobook, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the start panel is visible.
    /// </summary>
    public bool ShowStartPanel
    {
        get => _showStartPanel;
        private set => Set(ref _showStartPanel, value);
    }

    public bool ShowDebugMenu
    {
        get => _showDebugMenu;
        set => Set(ref _showDebugMenu, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the Audiobooks list is currently being updated.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the app is currently importing audiobooks.
    /// </summary>
    public bool IsImporting
    {
        get => _isImporting;
        set => Set(ref _isImporting, value);
    }

    public bool IsNavigationViewVisible
    {
        get => _isNavigationViewVisible;
        set => Set(ref _isNavigationViewVisible, value);
    }

    /// <summary>
    ///     Gets or sets the progress of the current import operation.
    /// </summary>
    public int ProgressDialogProgress
    {
        get => _progressDialogProgress;
        set => Set(ref _progressDialogProgress, value);
    }

    /// <summary>
    ///     Gets or sets the text to display while importing audiobooks.
    /// </summary>
    public string ProgressDialogText
    {
        get => _progressDialogText;
        set => Set(ref _progressDialogText, value);
    }

    /// <summary>
    ///     Gets or sets the prefix to display before the progress dialog text.
    /// </summary>
    public string ProgressDialogPrefix
    {
        get => _progressDialogPrefix;
        set => Set(ref _progressDialogPrefix, value);
    }

    /// <summary>
    ///     Gets or sets the total number of items to process in the progress dialog.
    /// </summary>
    public string ProgressDialogTotalText
    {
        get => _progressDialogTotalText;
        set => Set(ref _progressDialogTotalText, value);
    }

    // TODO: the following 2 properties are not needed anymore (unless the library list page is put back)

    /// <summary>
    ///     Gets or sets the text to display in the notification bar.
    /// </summary>
    public string NotificationText
    {
        get => _notificationText;
        set => Set(ref _notificationText, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the notification bar is visible.
    /// </summary>
    public bool IsNotificationBarVisible
    {
        get => _isNotificationBarVisible;
        set => Set(ref _isNotificationBarVisible, value);
    }

    public InfoBarSeverity NotificationSeverity
    {
        get => _notificationSeverity;
        set => Set(ref _notificationSeverity, value);
    }

    public ObservableCollection<SelectedFile> SelectedFiles { get; } = [];

    // TODO: need to move these methods to a separate class

    public ObservableCollection<Notification> Notifications { get; } = [];

    /// <summary>
    ///     Invoked when we want to reset the filters in the LibraryCardPage.
    /// </summary>
    public event ResetFiltersHandler? ResetFilters;

    /// <summary>
    ///     Gets the complete list of audiobooks from the database.
    /// </summary>
    public async Task GetAudiobookListAsync(bool firstRun = false)
    {
        try
        {
            ResetFilters?.Invoke();

            await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

            var audiobooks = (await App.Repository.Audiobooks.GetAsync()).AsList();

            await _dispatcherQueue.EnqueueAsync(() =>
            {
                ShowStartPanel = audiobooks.Count == 0;

                Audiobooks.Clear();
                AudiobooksForFilter.Clear();
                foreach (var audiobookViewModel in audiobooks.Select(c => new AudiobookViewModel(c)))
                {
                    Audiobooks.Add(audiobookViewModel);
                    AudiobooksForFilter.Add(audiobookViewModel);
                }

                if (firstRun)
                {
                    var nowPlaying = Audiobooks.FirstOrDefault(x => x.IsNowPlaying);
                    if (nowPlaying != null)
                        _ = App.PlayerViewModel.OpenAudiobook(nowPlaying);

                    // in the background check if there are any other audiobook records where IsNowPlaying is true and set them to false
                    Task.Run(async () =>
                    {
                        var otherNowPlaying = Audiobooks.Where(x => x.IsNowPlaying && x != nowPlaying).ToList();
                        foreach (var audiobook in otherNowPlaying)
                        {
                            audiobook.IsNowPlaying = false;
                            await App.Repository.Audiobooks.UpsertAsync(audiobook.Model);
                        }
                    }).ContinueWith(t =>
                    {
                        if (t.Exception != null)
                            // Handle the exception
                            LoggingService.LogError(t.Exception, true);
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }

                IsLoading = false;
            });
        }
        catch (Exception ex)
        {
            // Handle the exception
            LoggingService.LogError(ex, true);
        }
    }

    /// <summary>
    ///     Saves any modified audiobooks and reloads the audiobook list from the database.
    /// </summary>
    public void Refresh()
    {
        Task.Run(async () =>
        {
            // todo: do i need this?
            // foreach (var modifiedAudiobook in Audiobooks
            //              .Where(audiobook => audiobook.IsModified).Select(audiobook => audiobook.Model))
            //     await App.Repository.Audiobooks.UpsertAsync(modifiedAudiobook);

            await GetAudiobookListAsync();
        });
    }

    /// <summary>
    ///     Deletes the selected audiobook from the database.
    /// </summary>
    public async Task DeleteAudiobookAsync()
    {
        // todo: add a try-catch block here
        try
        {
            if (SelectedAudiobook == null) return;

            if (SelectedAudiobook == App.PlayerViewModel.NowPlaying)
                _dispatcherQueue.TryEnqueue(() =>
                {
                    App.PlayerViewModel.MediaPlayer.Pause();
                    App.PlayerViewModel.NowPlaying.IsNowPlaying = false;
                    App.PlayerViewModel.NowPlaying = null;
                });

            await App.Repository.Audiobooks.DeleteAsync(SelectedAudiobook.Id);
            await App.ViewModel.AppDataService.DeleteCoverImageAsync(SelectedAudiobook.CoverImagePath);

            await GetAudiobookListAsync();

            await _dispatcherQueue.EnqueueAsync(() =>
            {
                SelectedAudiobook = null;
                EnqueueNotification(new Notification
                {
                    Message = "Audiobook deleted successfully!",
                    Severity = InfoBarSeverity.Success
                });
            });
        }
        catch (Exception ex)
        {
            // Handle the exception
            LoggingService.LogError(ex, true);

            await DialogService.ShowErrorDialogAsync("Failed to delete audiobook", ex.Message);
        }
    }

    // todo: fix the bug here and add a confirmation dialog
    /// <summary>
    ///     Deletes all audiobooks from the database.
    /// </summary>
    public async void DeleteAudiobooksAsync()
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            App.PlayerViewModel.MediaPlayer.Pause();
            App.PlayerViewModel.NowPlaying = null;
            SelectedAudiobook = null;
            IsLoading = true;
        });

        var count = 0;
        foreach (var audiobook in Audiobooks)
        {
            await App.Repository.Audiobooks.DeleteAsync(audiobook.Id);
            await App.ViewModel.AppDataService.DeleteCoverImageAsync(audiobook.CoverImagePath);
            count++;
        }

        await GetAudiobookListAsync();

        EnqueueNotification(new Notification
        {
            Message = $"{count} Audiobooks deleted successfully!",
            Severity = InfoBarSeverity.Success
        });
    }

    /// <summary>
    ///     Resets the audiobook list.
    /// </summary>
    public async Task ResetAudiobookListAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
            await GetAudiobookListAsync());
    }

    /// <summary>
    ///     Enqueues a notification to be displayed in the InfoBar.
    /// </summary>
    /// <param name="notification"></param>
    public void EnqueueNotification(Notification notification)
    {
        _dispatcherQueue.EnqueueAsync(async () =>
        {
            Notifications.Add(notification);
            await Task.Delay(notification.Duration);
            Notifications.Remove(notification);
        });
    }

    /// <summary>
    ///     Invoked when a notification is closed.
    /// </summary>
    /// <param name="notification"></param>
    public void OnNotificationClosed(Notification notification)
    {
        _dispatcherQueue.EnqueueAsync(() =>
        {
            Notifications.Remove(notification);
            if (Notifications.Count == 0) Notifications.Clear();
        });
    }

    /// <summary>
    ///     Creates a JSON file containing all the audiobooks in the database.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void CreateExportFile(object sender, RoutedEventArgs e)
    {
        await GetAudiobookListAsync();

        var audiobooksExport = Audiobooks.Select(x => new
        {
            x.CurrentSourceFile.CurrentTimeMs, x.CoverImagePath, x.CurrentSourceFile.FilePath, x.Progress,
            x.CurrentChapterIndex, x.IsNowPlaying, x.IsCompleted
        });
        var json = JsonSerializer.Serialize(audiobooksExport);

        // let user choose where to save the file
        var savePicker = new FileSavePicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(savePicker, hWnd);
        savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
        savePicker.FileTypeChoices.Add("Audibly Export File", new List<string> { ".audibly" });
        savePicker.SuggestedFileName = "audibly_export";

        var file = savePicker.PickSaveFileAsync().AsTask().Result;

        if (file == null) return;

        // write the json string to the file
        FileIO.WriteTextAsync(file, json).AsTask().Wait();

        // notify the user that the file was created
        EnqueueNotification(new Notification
        {
            Message = "Export file created successfully!",
            Severity = InfoBarSeverity.Success
        });
    }

    /// <summary>
    ///     Migrates the database from the old schema to the new schema.
    /// </summary>
    public async Task MigrateDatabase()
    {
        var transaction = SentrySdk.StartTransaction("Data Migration", "Data Migration");
        try
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync("audibly_export.audibly");

            if (file == null)
            {
                UserSettings.ShowDataMigrationFailedDialog = true;
                return;
            }

            var json = await FileIO.ReadTextAsync(file);
            var importedAudiobooks = JsonSerializer.Deserialize<List<ImportedAudiobook>>(json);

            if (importedAudiobooks == null)
            {
                // log the error
                App.ViewModel.LoggingService.LogError(new Exception("Failed to deserialize the json file"), true);
                UserSettings.ShowDataMigrationFailedDialog = true;
                return;
            }

            // delete the old audiobooks from the database
            await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

            UpdateProgressDialogProperties(ProgressDialogPrefix = "Deleting audiobooks from old database");

            // yes, I'm intentionally not awaiting this
            // note: content dialog
            await DialogService.ShowProgressDialogAsync("Data Migration", _cancellationTokenSource, false);

            // check if the user has any audiobooks in the database (data migration was stopped midway)
            var audiobooks = await App.Repository.Audiobooks.GetAsync();
            if (audiobooks.Any())
                await App.Repository.Audiobooks.DeleteAllAsync(async (i, count, title, coverImagePath) =>
                {
                    // delete the cover image directory
                    await App.ViewModel.AppDataService.DeleteCoverImageAsync(coverImagePath);
                    UpdateProgressDialogProperties(progressDialogProgress: (int)((double)i / count * 100),
                        progressDialogPrefix: $"Deleting {title}:",
                        progressDialogText: $"{i} of {count}",
                        progressDialogTotalText: $"{i} of {count}");
                });

            UpdateProgressDialogProperties(ProgressDialogPrefix = "Starting cover image deletion");

            await AppDataService.DeleteCoverImagesAsync(
                importedAudiobooks.Select(x => x.CoverImagePath).ToList(),
                async (i, count, _) =>
                {
                    await _dispatcherQueue.EnqueueAsync(() =>
                    {
                        ProgressDialogProgress = ((double)i / count * 100).ToInt();
                        ProgressDialogPrefix = "Deleting audiobook";
                        ProgressDialogText = $"{i} of {count}";
                        ProgressDialogTotalText = $"{i} of {count}";
                    });
                });

            UpdateProgressDialogProperties(ProgressDialogPrefix = "Starting audiobook import");

            // notify user that we successfully deleted the old cover images
            EnqueueNotification(new Notification
            {
                Message = "Deleted audiobooks from old database",
                Severity = InfoBarSeverity.Success
            });

            // re-import the user's audiobooks
            await FileImporter.ImportFromJsonAsync(file, new CancellationToken(),
                async (i, count, title, _) =>
                {
                    await _dispatcherQueue.EnqueueAsync(() =>
                    {
                        ProgressDialogProgress = ((double)i / count * 100).ToInt();
                        ProgressDialogPrefix = "Importing";
                        ProgressDialogText = $"{title}";
                        ProgressDialogTotalText = $"{i} of {count}";
                    });
                });

            UpdateProgressDialogProperties();

            // notify user that we successfully imported their audiobooks
            EnqueueNotification(new Notification
            {
                Message = "Data migration completed successfully",
                Severity = InfoBarSeverity.Success
            });
        }
        catch (Exception exception)
        {
            UserSettings.ShowDataMigrationFailedDialog = true;

            // log the error
            LoggingService.LogError(exception, true);

            // notify user that we failed to import their audiobooks
            EnqueueNotification(new Notification
            {
                Message = "Data Migration Failed",
                Severity = InfoBarSeverity.Error
            });
        }
        finally
        {
            await DialogService.CloseProgressDialogAsync();

            UserSettings.NeedToImportAudiblyExport = false;

            transaction.Finish();

            if (UserSettings.ShowDataMigrationFailedDialog)
            {
                UserSettings.ShowDataMigrationFailedDialog = false;

                // note: content dialog
                await DialogService.ShowDataMigrationFailedDialogAsync();
            }

            await GetAudiobookListAsync(true);
        }
    }

    private async void UpdateProgressDialogProperties(string progressDialogText = "", int progressDialogProgress = 0,
        string progressDialogPrefix = "", string progressDialogTotalText = "")
    {
        await _dispatcherQueue.EnqueueAsync(() =>
        {
            ProgressDialogText = progressDialogText;
            ProgressDialogProgress = progressDialogProgress;
            ProgressDialogPrefix = progressDialogPrefix;
            ProgressDialogTotalText = progressDialogTotalText;
        });
    }

    // private double _titleFontSizeIncrement = 2;
    // private double _titleMaxWidthIncrement = 37.5;
    // private double _playButtonHeightWidthIncrement = 10;
    // private double _isCompleteCheckmarkFontSizeIncrement = 4;
    // private double _audiobookTileWidthIncrement = 50;

    private void AnimateProperty(DependencyObject target, string propertyPath, double toValue, double durationSeconds)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = toValue,
            Duration = new Duration(TimeSpan.FromSeconds(durationSeconds))
        };

        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    public void IncreaseAudiobookTileSize()
    {
        var newTitleFontSize = TitleFontSize + _titleFontSizeIncrement > _maximumTitleFontSize
            ? _maximumTitleFontSize
            : TitleFontSize + _titleFontSizeIncrement;

        var newAuthorFontSize = AuthorFontSize + _authorFontSizeIncrement > _maximumAuthorFontSize
            ? _maximumAuthorFontSize
            : AuthorFontSize + _authorFontSizeIncrement;

        var newTitleMaxWidth = TitleMaxWidth + _titleMaxWidthIncrement > _maximumTitleMaxWidth
            ? _maximumTitleMaxWidth
            : TitleMaxWidth + _titleMaxWidthIncrement;

        var newPlayButtonHeightWidth =
            PlayButtonHeightWidth + _playButtonHeightWidthIncrement > _maximumPlayButtonHeightWidth
                ? _maximumPlayButtonHeightWidth
                : PlayButtonHeightWidth + _playButtonHeightWidthIncrement;

        var newProgressIndicatorTextFontSize = ProgressIndicatorTextFontSize + _progressIndicatorTextFontSizeIncrement >
                                               _maximumProgressIndicatorTextFontSize
            ? _maximumProgressIndicatorTextFontSize
            : ProgressIndicatorTextFontSize + _progressIndicatorTextFontSizeIncrement;

        var newProgressIndicatorFontSize = ProgressIndicatorFontSize + _progressIndicatorFontSizeIncrement >
                                           _maximumProgressIndicatorFontSize
            ? _maximumProgressIndicatorFontSize
            : ProgressIndicatorFontSize + _progressIndicatorFontSizeIncrement;

        var newAudiobookTileWidth = AudiobookTileWidth + _audiobookTileWidthIncrement > _maximumAudiobookTileWidth
            ? _maximumAudiobookTileWidth
            : AudiobookTileWidth + _audiobookTileWidthIncrement;

        _dispatcherQueue.TryEnqueue(() =>
        {
            TitleFontSize = newTitleFontSize;
            AuthorFontSize = newAuthorFontSize;
            TitleMaxWidth = newTitleMaxWidth;
            PlayButtonHeightWidth = newPlayButtonHeightWidth;
            ProgressIndicatorTextFontSize = newProgressIndicatorTextFontSize;
            ProgressIndicatorFontSize = newProgressIndicatorFontSize;
            AudiobookTileWidth = newAudiobookTileWidth;
            
            // AnimateProperty(this, nameof(TitleFontSize), newTitleFontSize, 0.5);
            // AnimateProperty(this, nameof(AuthorFontSize), newAuthorFontSize, 0.5);
            // AnimateProperty(this, nameof(TitleMaxWidth), newTitleMaxWidth, 0.5);
            // AnimateProperty(this, nameof(PlayButtonHeightWidth), newPlayButtonHeightWidth, 0.5);
            // AnimateProperty(this, nameof(ProgressIndicatorTextFontSize), newProgressIndicatorTextFontSize, 0.5);
            // AnimateProperty(this, nameof(ProgressIndicatorFontSize), newProgressIndicatorFontSize, 0.5);
            // AnimateProperty(this, nameof(AudiobookTileWidth), newAudiobookTileWidth, 0.5);
        });
        
        Debug.WriteLine($"TitleFontSize: {newTitleFontSize}");
        Debug.WriteLine($"AuthorFontSize: {newAuthorFontSize}");
        Debug.WriteLine($"TitleMaxWidth: {newTitleMaxWidth}");
        Debug.WriteLine($"PlayButtonHeightWidth: {newPlayButtonHeightWidth}");
        Debug.WriteLine($"ProgressIndicatorTextFontSize: {newProgressIndicatorTextFontSize}");
        Debug.WriteLine($"ProgressIndicatorFontSize: {newProgressIndicatorFontSize}");
        Debug.WriteLine($"AudiobookTileWidth: {newAudiobookTileWidth}");
    }

    public void DecreaseAudiobookTileSize()
    {
        var newTitleFontSize = TitleFontSize - _titleFontSizeIncrement < _minimumTitleFontSize
            ? _minimumTitleFontSize
            : TitleFontSize - _titleFontSizeIncrement;

        var newAuthorFontSize = AuthorFontSize - _authorFontSizeIncrement < _minimumAuthorFontSize
            ? _minimumAuthorFontSize
            : AuthorFontSize - _authorFontSizeIncrement;

        var newTitleMaxWidth = TitleMaxWidth - _titleMaxWidthIncrement < _minimumTitleMaxWidth
            ? _minimumTitleMaxWidth
            : TitleMaxWidth - _titleMaxWidthIncrement;

        var newPlayButtonHeightWidth =
            PlayButtonHeightWidth - _playButtonHeightWidthIncrement < _minimumPlayButtonHeightWidth
                ? _minimumPlayButtonHeightWidth
                : PlayButtonHeightWidth - _playButtonHeightWidthIncrement;

        var newProgressIndicatorTextFontSize = ProgressIndicatorTextFontSize - _progressIndicatorTextFontSizeIncrement <
                                               _minimumProgressIndicatorTextFontSize
            ? _minimumProgressIndicatorTextFontSize
            : ProgressIndicatorTextFontSize - _progressIndicatorTextFontSizeIncrement;

        var newProgressIndicatorFontSize = ProgressIndicatorFontSize - _progressIndicatorFontSizeIncrement <
                                           _minimumProgressIndicatorFontSize
            ? _minimumProgressIndicatorFontSize
            : ProgressIndicatorFontSize - _progressIndicatorFontSizeIncrement;

        var newAudiobookTileWidth = AudiobookTileWidth - _audiobookTileWidthIncrement < _minimumAudiobookTileWidth
            ? _minimumAudiobookTileWidth
            : AudiobookTileWidth - _audiobookTileWidthIncrement;

        _dispatcherQueue.TryEnqueue(() =>
        {
            TitleFontSize = newTitleFontSize;
            AuthorFontSize = newAuthorFontSize;
            TitleMaxWidth = newTitleMaxWidth;
            PlayButtonHeightWidth = newPlayButtonHeightWidth;
            ProgressIndicatorTextFontSize = newProgressIndicatorTextFontSize;
            ProgressIndicatorFontSize = newProgressIndicatorFontSize;
            AudiobookTileWidth = newAudiobookTileWidth;
            
            // AnimateProperty(this, nameof(TitleFontSize), newTitleFontSize, 0.5);
            // AnimateProperty(this, nameof(AuthorFontSize), newAuthorFontSize, 0.5);
            // AnimateProperty(this, nameof(TitleMaxWidth), newTitleMaxWidth, 0.5);
            // AnimateProperty(this, nameof(PlayButtonHeightWidth), newPlayButtonHeightWidth, 0.5);
            // AnimateProperty(this, nameof(ProgressIndicatorTextFontSize), newProgressIndicatorTextFontSize, 0.5);
            // AnimateProperty(this, nameof(ProgressIndicatorFontSize), newProgressIndicatorFontSize, 0.5);
            // AnimateProperty(this, nameof(AudiobookTileWidth), newAudiobookTileWidth, 0.5);
        });
        
        Debug.WriteLine($"TitleFontSize: {newTitleFontSize}");
        Debug.WriteLine($"AuthorFontSize: {newAuthorFontSize}");
        Debug.WriteLine($"TitleMaxWidth: {newTitleMaxWidth}");
        Debug.WriteLine($"PlayButtonHeightWidth: {newPlayButtonHeightWidth}");
        Debug.WriteLine($"ProgressIndicatorTextFontSize: {newProgressIndicatorTextFontSize}");
        Debug.WriteLine($"ProgressIndicatorFontSize: {newProgressIndicatorFontSize}");
        Debug.WriteLine($"AudiobookTileWidth: {newAudiobookTileWidth}");
    }

    #region Properties for resizing audiobook tiles

    private double _titleFontSize;

    public double TitleFontSize
    {
        get => _titleFontSize;
        set => Set(ref _titleFontSize, value);
    }

    private double _authorFontSize;

    public double AuthorFontSize
    {
        get => _authorFontSize;
        set => Set(ref _authorFontSize, value);
    }

    private double _titleMaxWidth;

    public double TitleMaxWidth
    {
        get => _titleMaxWidth;
        set => Set(ref _titleMaxWidth, value);
    }

    private double _playButtonHeightWidth;

    public double PlayButtonHeightWidth
    {
        get => _playButtonHeightWidth;
        set => Set(ref _playButtonHeightWidth, value);
    }

    private double _progressIndicatorTextFontSize;

    public double ProgressIndicatorTextFontSize
    {
        get => _progressIndicatorTextFontSize;
        set => Set(ref _progressIndicatorTextFontSize, value);
    }

    private double _progressIndicatorFontSize;

    public double ProgressIndicatorFontSize
    {
        get => _progressIndicatorFontSize;
        set => Set(ref _progressIndicatorFontSize, value);
    }

    private double _audiobookTileMinWidth;

    public double AudiobookTileWidth
    {
        get => _audiobookTileMinWidth;
        set => Set(ref _audiobookTileMinWidth, value);
    }

    private double _audiobookTileMinColumnSpacing;

    public double AudiobookTileMinColumnSpacing
    {
        get => _audiobookTileMinColumnSpacing;
        set => Set(ref _audiobookTileMinColumnSpacing, value);
    }

    #endregion

    # region File Import Operations

    public async void ImportAudiobookAsync()
    {
        var openPicker = new FileOpenPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add(".m4b");
        openPicker.FileTypeFilter.Add(".mp3");

        var file = await openPicker.PickSingleFileAsync();
        if (file == null) return;

        await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        UpdateProgressDialogProperties(ProgressDialogPrefix = "Importing");

        // note: content dialog
        await DialogService.ShowProgressDialogAsync("Importing Audiobook", _cancellationTokenSource);

        await ImportFileAsync(file, token);
    }

    public async Task ImportAudiobookFromFileActivationAsync(string path, bool showImportDialog = true)
    {
        await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        if (showImportDialog)
        {
            UpdateProgressDialogProperties(ProgressDialogPrefix = "Importing");

            // note: content dialog
            await DialogService.ShowProgressDialogAsync("Importing Audiobook", _cancellationTokenSource);
        }

        var file = await StorageFile.GetFileFromPathAsync(path);

        await ImportFileAsync(file, token);
    }

    private async Task ImportFileAsync(StorageFile file, CancellationToken token)
    {
        var importFailed = false;
        try
        {
            async Task ProgressCallback(int progress, int total, string title, bool didFail)
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    ProgressDialogProgress = (int)((double)progress / total * 100);
                    ProgressDialogPrefix = "Importing";
                    ProgressDialogText = title;
                    ProgressDialogTotalText = $"{progress} of {total}";
                });

                if (didFail)
                {
                    importFailed = true;
                    EnqueueNotification(new Notification
                    {
                        Message = "Failed to import audiobook. Path: " + file.Path, Severity = InfoBarSeverity.Error
                    });
                }
            }

            await FileImporter.ImportFileAsync(file.Path, token, ProgressCallback);
        }
        catch (OperationCanceledException)
        {
            EnqueueNotification(new Notification
            {
                Message = "Import operation was cancelled!", Severity = InfoBarSeverity.Warning
            });
        }
        catch (Exception e)
        {
            importFailed = true;
            EnqueueNotification(new Notification
            {
                Message = "Failed to import audiobook. Path: " + file.Path,
                Severity = InfoBarSeverity.Error
            });
            LoggingService.Log(e.Message);
        }

        await DialogService.CloseProgressDialogAsync();

        if (!importFailed)
            EnqueueNotification(new Notification
            {
                Message = "Audiobook imported successfully!",
                Severity = InfoBarSeverity.Success
            });

        await GetAudiobookListAsync();

        // select the imported audiobook
        var audiobook = Audiobooks.FirstOrDefault(a => a.CurrentSourceFile.FilePath == file.Path);
        if (audiobook != null)
            await App.PlayerViewModel.OpenAudiobook(audiobook);
    }

    public async void ImportAudiobooksFromDirectoryAsync()
    {
        var openPicker = new FolderPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add("*");

        var folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
        else
            return;

        // todo: idk if i want the loading progress bar to be shown or not
        await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        UpdateProgressDialogProperties(ProgressDialogPrefix = "Importing");

        var totalBooks = 0;

        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            // note: content dialog
            await DialogService.ShowProgressDialogAsync("Importing Audiobooks", _cancellationTokenSource);

            async Task ProgressCallback(int progress, int total, string title, bool didFail)
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    totalBooks++;
                    ProgressDialogProgress = ((double)progress / total * 100).ToInt();
                    ProgressDialogPrefix = "Importing";
                    ProgressDialogText = title;
                    ProgressDialogTotalText = $"{progress} of {total}";
                });

                if (didFail)
                {
                    totalBooks--;
                    EnqueueNotification(new Notification
                        { Message = $"Failed to import {title}!", Severity = InfoBarSeverity.Error });
                }
            }

            await FileImporter.ImportDirectoryAsync(folder.Path, token, ProgressCallback);
        }
        catch (OperationCanceledException)
        {
            EnqueueNotification(new Notification
            {
                Message = "Import operation was cancelled!", Severity = InfoBarSeverity.Warning
            });
        }
        catch (Exception e)
        {
            EnqueueNotification(new Notification
            {
                Message = "Failed to import audiobooks!", Severity = InfoBarSeverity.Error
            });
            LoggingService.LogError(e, true);
        }

        await DialogService.CloseProgressDialogAsync();

        await _dispatcherQueue.EnqueueAsync(() =>
        {
            ProgressDialogText = string.Empty;
            ProgressDialogProgress = 0;
        });

        EnqueueNotification(new Notification
        {
            Message = $"{totalBooks} Audiobooks imported successfully!", Severity = InfoBarSeverity.Success
        });

        await GetAudiobookListAsync();

        stopwatch.Stop();
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    public async Task ImportAudiobookWithMultipleFilesAsync(object sender, RoutedEventArgs e)
    {
        var openPicker = new FileOpenPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add(".m4b");
        openPicker.FileTypeFilter.Add(".mp3");

        var files = await openPicker.PickMultipleFilesAsync();
        if (files.IsNullOrEmpty()) return;

        // add the selected files to the observable collection
        foreach (var file in files)
            SelectedFiles.Add(new SelectedFile(filePath: file.Path, fileName: file.Name));

        await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        // get framework element from sender
        var element = sender as FrameworkElement;
        if (element == null) return;

        // note: content dialog
        var result = await DialogService.ShowSelectFilesDialogAsync();
        if (result == ContentDialogResult.None)
        {
            SelectedFiles.Clear();
            await _dispatcherQueue.EnqueueAsync(() => IsLoading = false);
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        UpdateProgressDialogProperties(ProgressDialogPrefix = "Importing");

        var totalBooks = 0;
        var failedBooks = 0;

        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            // note: content dialog
            await DialogService.ShowProgressDialogAsync("Importing Audiobooks", _cancellationTokenSource);

            var filesArray = SelectedFiles.Select(file => file.FilePath).ToArray();

            async Task ProgressCallback(int progress, int total, string title, bool didFail)
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    totalBooks++;
                    ProgressDialogProgress = ((double)progress / total * 100).ToInt();
                    ProgressDialogPrefix = "Importing";
                    ProgressDialogText = title;
                    ProgressDialogTotalText = $"{progress} of {total}";
                });

                if (didFail)
                {
                    totalBooks--;
                    failedBooks++;
                    EnqueueNotification(new Notification
                        { Message = $"Failed to import {title}!", Severity = InfoBarSeverity.Error });
                }
            }

            await FileImporter.ImportFromMultipleFilesAsync(filesArray, token, ProgressCallback);
        }
        catch (OperationCanceledException)
        {
            EnqueueNotification(new Notification
            {
                Message = "Import operation was cancelled!", Severity = InfoBarSeverity.Warning
            });
        }
        catch (Exception exception)
        {
            EnqueueNotification(new Notification
            {
                Message = "Failed to import audiobooks!", Severity = InfoBarSeverity.Error
            });
            LoggingService.LogError(exception, true);
        }
        finally
        {
            // clear selected files
            SelectedFiles.Clear();
        }

        await DialogService.CloseProgressDialogAsync();

        if (failedBooks == 0)
            EnqueueNotification(new Notification
            {
                Message = $"{totalBooks} Audiobooks imported successfully!", Severity = InfoBarSeverity.Success
            });

        await GetAudiobookListAsync();

        stopwatch.Stop();
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed} seconds.");
    }

    /// <summary>
    ///     Imports audiobooks from a user-selected JSON file.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public async void ImportFromUserSelectedJsonFileAsync(object sender, RoutedEventArgs e)
    {
        var openPicker = new FileOpenPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add(".audibly");

        var file = await openPicker.PickSingleFileAsync();
        if (file == null) return;

        await ImportFromJsonFileAsync(file);
    }

    /// <summary>
    ///     Imports audiobooks from a JSON file.
    /// </summary>
    /// <param name="file"></param>
    public async Task ImportFromJsonFileAsync(StorageFile file)
    {
        await _dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        UpdateProgressDialogProperties(ProgressDialogPrefix = "Importing");

        var totalBooks = 0;
        var failedBooks = 0;

        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            // note: content dialog
            await DialogService.ShowProgressDialogAsync("Importing Audiobooks", _cancellationTokenSource);

            async Task ProgressCallback(int progress, int total, string title, bool didFail)
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    totalBooks++;
                    ProgressDialogProgress = ((double)progress / total * 100).ToInt();
                    ProgressDialogPrefix = "Importing";
                    ProgressDialogText = title;
                    ProgressDialogTotalText = $"{progress} of {total}";
                });

                if (didFail)
                {
                    totalBooks--;
                    failedBooks++;
                    EnqueueNotification(new Notification
                        { Message = $"Failed to import {title}!", Severity = InfoBarSeverity.Error });
                }
            }

            await FileImporter.ImportFromJsonAsync(file, token, ProgressCallback);
        }
        catch (OperationCanceledException)
        {
            EnqueueNotification(new Notification
            {
                Message = "Import operation was cancelled!", Severity = InfoBarSeverity.Warning
            });
        }
        catch (Exception e)
        {
            EnqueueNotification(new Notification
            {
                Message = "Failed to import audiobooks!", Severity = InfoBarSeverity.Error
            });
            LoggingService.LogError(e, true);
        }

        await DialogService.CloseProgressDialogAsync();

        if (failedBooks > 0)
            EnqueueNotification(new Notification
            {
                Message = $"{failedBooks} Audiobooks failed to import!", Severity = InfoBarSeverity.Error
            });

        if (totalBooks > 0)
            EnqueueNotification(new Notification
            {
                Message = $"{totalBooks} Audiobooks imported successfully!", Severity = InfoBarSeverity.Success
            });

        await GetAudiobookListAsync(true);

        stopwatch.Stop();
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed} seconds.");
    }

    #endregion
}

public class Notification
{
    public string Message { get; init; }
    public InfoBarSeverity Severity { get; init; }
    public TimeSpan Duration { get; } = TimeSpan.FromSeconds(20);
}