// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

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
using Audibly.App.ViewModels.Interfaces;
using Audibly.Models;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sentry;
using Sharpener.Extensions;
using WinRT.Interop;
using Constants = Audibly.App.Helpers.Constants;

namespace Audibly.App.ViewModels;

/// <summary>
///     Provides data and commands accessible to the entire app.
/// </summary>
public class MainViewModel : BindableBase
{
    #region Delegates

    public delegate void ResetFiltersHandler();

    #endregion

    private readonly SemaphoreSlim _audiobookTileSizeSemaphore = new(1, 1);

    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public readonly IAppDataService AppDataService;
    public readonly IImportFiles FileImporter;
    public readonly IloggingService LoggingService;

    private CancellationTokenSource? _cancellationTokenSource;

    private bool _isImporting;
    private bool _isLoading;
    private bool _isNavigationViewVisible = true;
    private string _progressDialogPrefix = string.Empty;
    private int _progressDialogProgress;
    private string _progressDialogText = string.Empty;
    private string _progressDialogTotalText = string.Empty;
    private AudiobookViewModel? _selectedAudiobook;
    private bool _showAlignmentGrids;
    private bool _showDebugMenu;
    private bool _showStartPanel;
    private bool _zoomInButtonIsEnabled = true;
    private double _zoomLevel;
    private bool _zoomOutButtonIsEnabled = true;

    /// <summary>
    ///     Creates a new MainViewModel.
    /// </summary>
    public MainViewModel(IImportFiles fileImporter, IAppDataService appDataService, IloggingService loggingService)
    {
        FileImporter = fileImporter;
        AppDataService = appDataService;
        LoggingService = loggingService;
        // Task.Run(() => GetAudiobookListAsync(true));
        // Task.Run(GetFileSystemItemsAsync);

        // todo: save this as a user setting
        TitleFontSize = 18; // 1.8
        AuthorFontSize = 14; // 1.4
        TitleMaxWidth = 200; // 20
        PlayButtonHeightWidth = 70; // 7
        ProgressIndicatorTextFontSize = 20; // 2
        ProgressIndicatorFontSize = 40; // 4
        AudiobookTileWidth = 300; // 30
        AudiobookTileMinColumnSpacing = 28; // 2.8

        ZoomLevel = 100;

        InitializeZoomLevelToTileSizeDictionary();
    }

    public string FileActivationError { get; set; } = string.Empty;

    /// <summary>
    ///     The collection of audiobooks in the list.
    /// </summary>
    public ObservableCollection<AudiobookViewModel> Audiobooks { get; } = [];


    /// <summary>
    ///     The collection of folders and audiobooks for the collections page.
    /// </summary>
    public ObservableCollection<IFileSystemItem> FileSystemItems { get; } = [];

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

    public bool ShowAlignmentGrids
    {
        get => _showAlignmentGrids;
        set => Set(ref _showAlignmentGrids, value);
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        set => Set(ref _zoomLevel, value);
    }

    public bool ZoomInButtonIsEnabled
    {
        get => _zoomInButtonIsEnabled;
        set => Set(ref _zoomInButtonIsEnabled, value);
    }

    public bool ZoomOutButtonIsEnabled
    {
        get => _zoomOutButtonIsEnabled;
        set => Set(ref _zoomOutButtonIsEnabled, value);
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

    public ObservableCollection<SelectedFile> SelectedFiles { get; } = [];

    // TODO: need to move these methods to a separate class

    public ObservableCollection<Notification> Notifications { get; } = [];

    public Guid CurrentFolderId { get; set; }

    /// <summary>
    ///     Invoked when we want to reset the filters in the LibraryCardPage.
    /// </summary>
    public event ResetFiltersHandler? ResetFilters;

    public async Task GetFileSystemItemsAsync()
    {
        var collections = await App.Repository.Collections.GetAllChildrenAsync(Constants.DefaultFolderId);
        var collectionViewModels = collections.Select(f => new CollectionViewModel(f));

        // Load audiobooks (replace with your actual logic)
        var audiobooks = await App.Repository.Audiobooks.GetAsync();
        var audiobookViewModels = audiobooks.Select(a => new AudiobookViewModel(a));

        // Combine collections and audiobooks into a single collection
        FileSystemItems.Clear();
        foreach (var folder in collectionViewModels) FileSystemItems.Add(folder);
        foreach (var audiobook in audiobookViewModels) FileSystemItems.Add(audiobook);
    }

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

    public void ResizeAudiobookTile(double size)
    {
        _audiobookTileSizeSemaphore.Wait();

        try
        {
            ZoomLevel = size;
            if (_zoomLevelToTileSize.TryGetValue((int)ZoomLevel, out var tileSize))
                _dispatcherQueue.TryEnqueue(() =>
                {
                    TitleFontSize = tileSize.TitleFontSize;
                    AuthorFontSize = tileSize.AuthorFontSize;
                    TitleMaxWidth = tileSize.TitleMaxWidth;
                    PlayButtonHeightWidth = tileSize.PlayButtonHeightWidth;
                    ProgressIndicatorTextFontSize = tileSize.ProgressIndicatorTextFontSize;
                    ProgressIndicatorFontSize = tileSize.ProgressIndicatorFontSize;
                    AudiobookTileWidth = tileSize.AudiobookTileWidth;
                    AudiobookTileMinColumnSpacing = tileSize.AudiobookTileMinColumnSpacing;
                });
        }
        finally
        {
            _audiobookTileSizeSemaphore.Release();
        }
    }

    public void IncreaseAudiobookTileSize()
    {
        if (ZoomLevel >= 180)
        {
            ZoomLevel = 180;
            return;
        }

        ZoomLevel += 10;
        ZoomInButtonIsEnabled = ZoomLevel < 180;
        ZoomOutButtonIsEnabled = ZoomLevel > 50;

        ResizeAudiobookTile(ZoomLevel);
    }

    public void DecreaseAudiobookTileSize()
    {
        if (ZoomLevel <= 50)
        {
            ZoomLevel = 50;
            return;
        }

        ZoomLevel -= 10;
        ZoomInButtonIsEnabled = ZoomLevel < 180;
        ZoomOutButtonIsEnabled = ZoomLevel > 50;

        ResizeAudiobookTile(ZoomLevel);
    }

    #region Folder Operations

    public async void CreateCollectionAsync(string name, Guid? parentFolderId = null)
    {
        try
        {
            var folder = new Collection
            {
                Name = name,
                ParentFolderId = parentFolderId ?? Constants.DefaultFolderId
            };

            await App.Repository.Collections.UpsertAsync(folder);

            EnqueueNotification(new Notification
            {
                Message = "Folder created successfully!",
                Severity = InfoBarSeverity.Success
            });
        }
        catch (Exception e)
        {
            LoggingService.LogError(e, true);
            EnqueueNotification(new Notification
            {
                Message = "Failed to create folder!",
                Severity = InfoBarSeverity.Error
            });
        }
    }

    #endregion

    #region Properties for resizing audiobook tiles

    private Dictionary<int, AudiobookTileSize> _zoomLevelToTileSize;

    private void InitializeZoomLevelToTileSizeDictionary()
    {
        _zoomLevelToTileSize = new Dictionary<int, AudiobookTileSize>
        {
            {
                50,
                new AudiobookTileSize
                {
                    TitleFontSize = 9,
                    AuthorFontSize = 7,
                    TitleMaxWidth = 100,
                    PlayButtonHeightWidth = 35,
                    ProgressIndicatorTextFontSize = 10,
                    ProgressIndicatorFontSize = 20,
                    AudiobookTileWidth = 150,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                60,
                new AudiobookTileSize
                {
                    TitleFontSize = 10.8,
                    AuthorFontSize = 8.4,
                    TitleMaxWidth = 120,
                    PlayButtonHeightWidth = 42,
                    ProgressIndicatorTextFontSize = 12,
                    ProgressIndicatorFontSize = 24,
                    AudiobookTileWidth = 180,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                70,
                new AudiobookTileSize
                {
                    TitleFontSize = 12.6,
                    AuthorFontSize = 9.8,
                    TitleMaxWidth = 140,
                    PlayButtonHeightWidth = 49,
                    ProgressIndicatorTextFontSize = 14,
                    ProgressIndicatorFontSize = 28,
                    AudiobookTileWidth = 210,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                80,
                new AudiobookTileSize
                {
                    TitleFontSize = 14.4,
                    AuthorFontSize = 11.2,
                    TitleMaxWidth = 160,
                    PlayButtonHeightWidth = 56,
                    ProgressIndicatorTextFontSize = 16,
                    ProgressIndicatorFontSize = 32,
                    AudiobookTileWidth = 240,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                90,
                new AudiobookTileSize
                {
                    TitleFontSize = 16.2,
                    AuthorFontSize = 12.6,
                    TitleMaxWidth = 180,
                    PlayButtonHeightWidth = 63,
                    ProgressIndicatorTextFontSize = 18,
                    ProgressIndicatorFontSize = 36,
                    AudiobookTileWidth = 270,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                100,
                new AudiobookTileSize
                {
                    TitleFontSize = 18,
                    AuthorFontSize = 14,
                    TitleMaxWidth = 200,
                    PlayButtonHeightWidth = 70,
                    ProgressIndicatorTextFontSize = 20,
                    ProgressIndicatorFontSize = 40,
                    AudiobookTileWidth = 300,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                110,
                new AudiobookTileSize
                {
                    TitleFontSize = 19.8,
                    AuthorFontSize = 15.4,
                    TitleMaxWidth = 220,
                    PlayButtonHeightWidth = 77,
                    ProgressIndicatorTextFontSize = 22,
                    ProgressIndicatorFontSize = 44,
                    AudiobookTileWidth = 330,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                120,
                new AudiobookTileSize
                {
                    TitleFontSize = 21.6,
                    AuthorFontSize = 16.8,
                    TitleMaxWidth = 240,
                    PlayButtonHeightWidth = 84,
                    ProgressIndicatorTextFontSize = 24,
                    ProgressIndicatorFontSize = 48,
                    AudiobookTileWidth = 360,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                130,
                new AudiobookTileSize
                {
                    TitleFontSize = 23.4,
                    AuthorFontSize = 18.2,
                    TitleMaxWidth = 260,
                    PlayButtonHeightWidth = 91,
                    ProgressIndicatorTextFontSize = 26,
                    ProgressIndicatorFontSize = 52,
                    AudiobookTileWidth = 390,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                140,
                new AudiobookTileSize
                {
                    TitleFontSize = 25.2,
                    AuthorFontSize = 19.6,
                    TitleMaxWidth = 280,
                    PlayButtonHeightWidth = 98,
                    ProgressIndicatorTextFontSize = 28,
                    ProgressIndicatorFontSize = 56,
                    AudiobookTileWidth = 420,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                150,
                new AudiobookTileSize
                {
                    TitleFontSize = 27,
                    AuthorFontSize = 21,
                    TitleMaxWidth = 300,
                    PlayButtonHeightWidth = 105,
                    ProgressIndicatorTextFontSize = 30,
                    ProgressIndicatorFontSize = 60,
                    AudiobookTileWidth = 450,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                160,
                new AudiobookTileSize
                {
                    TitleFontSize = 28.8,
                    AuthorFontSize = 22.4,
                    TitleMaxWidth = 320,
                    PlayButtonHeightWidth = 112,
                    ProgressIndicatorTextFontSize = 32,
                    ProgressIndicatorFontSize = 64,
                    AudiobookTileWidth = 480,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                170,
                new AudiobookTileSize
                {
                    TitleFontSize = 30.6,
                    AuthorFontSize = 23.8,
                    TitleMaxWidth = 340,
                    PlayButtonHeightWidth = 119,
                    ProgressIndicatorTextFontSize = 34,
                    ProgressIndicatorFontSize = 68,
                    AudiobookTileWidth = 510,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                180,
                new AudiobookTileSize
                {
                    TitleFontSize = 32.4,
                    AuthorFontSize = 25.2,
                    TitleMaxWidth = 360,
                    PlayButtonHeightWidth = 126,
                    ProgressIndicatorTextFontSize = 36,
                    ProgressIndicatorFontSize = 72,
                    AudiobookTileWidth = 540,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                190,
                new AudiobookTileSize
                {
                    TitleFontSize = 34.2,
                    AuthorFontSize = 26.6,
                    TitleMaxWidth = 380,
                    PlayButtonHeightWidth = 133,
                    ProgressIndicatorTextFontSize = 38,
                    ProgressIndicatorFontSize = 76,
                    AudiobookTileWidth = 570,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            },
            {
                200,
                new AudiobookTileSize
                {
                    TitleFontSize = 36,
                    AuthorFontSize = 28,
                    TitleMaxWidth = 400,
                    PlayButtonHeightWidth = 140,
                    ProgressIndicatorTextFontSize = 40,
                    ProgressIndicatorFontSize = 80,
                    AudiobookTileWidth = 600,
                    AudiobookTileMinColumnSpacing = 2.8
                }
            }
        };

        ResizeAudiobookTile(100);
    }

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
    private async Task ImportFromJsonFileAsync(StorageFile file)
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

public class AudiobookTileSize
{
    public double TitleFontSize { get; set; }
    public double AuthorFontSize { get; set; }
    public double TitleMaxWidth { get; set; }
    public double PlayButtonHeightWidth { get; set; }
    public double ProgressIndicatorTextFontSize { get; set; }
    public double ProgressIndicatorFontSize { get; set; }
    public double AudiobookTileWidth { get; set; }
    public double AudiobookTileMinColumnSpacing { get; set; }
}