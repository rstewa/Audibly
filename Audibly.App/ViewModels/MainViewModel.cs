// Author: rstewa · https://github.com/rstewa
// Created: 4/15/2024
// Updated: 4/17/2024

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Audibly.App.Extensions;
using Audibly.App.Helpers;
using Audibly.App.Services;
using Audibly.App.Services.Interfaces;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Sharpener.Extensions;
using WinRT.Interop;

namespace Audibly.App.ViewModels;

/// <summary>
///     Provides data and commands accessible to the entire app.
/// </summary>
public class MainViewModel : BindableBase
{
    private readonly DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    public readonly IImportFiles FileImporter;
    public readonly IAppDataService AppDataService;
    public readonly MessageService MessageService;
    public readonly IloggingService LoggingService;

    /// <summary>
    ///     Creates a new MainViewModel.
    /// </summary>
    public MainViewModel(IImportFiles fileImporter, IAppDataService appDataService, MessageService messageService,
        IloggingService loggingService)
    {
        FileImporter = fileImporter;
        AppDataService = appDataService;
        MessageService = messageService;
        LoggingService = loggingService;
        Task.Run(GetAudiobookListAsync);
    }

    /// <summary>
    ///     The collection of audiobooks in the list.
    /// </summary>
    public ObservableCollection<AudiobookViewModel> Audiobooks { get; } = [];

    private AudiobookViewModel? _selectedAudiobook;

    /// <summary>
    ///     Gets or sets the selected audiobook, or null if no audiobook is selected.
    /// </summary>
    public AudiobookViewModel? SelectedAudiobook
    {
        get => _selectedAudiobook;
        set => Set(ref _selectedAudiobook, value);
    }

    private bool _showStartPanel;

    /// <summary>
    ///     Gets or sets a value indicating whether the start panel is visible.
    /// </summary>
    public bool ShowStartPanel
    {
        get => _showStartPanel;
        private set => Set(ref _showStartPanel, value);
    }

    private bool _showDebugMenu;

    public bool ShowDebugMenu
    {
        get => _showDebugMenu;
        set => Set(ref _showDebugMenu, value);
    }

    private bool _isLoading;

    /// <summary>
    ///     Gets or sets a value indicating whether the Audiobooks list is currently being updated.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    private bool _isImporting;

    /// <summary>
    ///     Gets or sets a value indicating whether the app is currently importing audiobooks.
    /// </summary>
    public bool IsImporting
    {
        get => _isImporting;
        set => Set(ref _isImporting, value);
    }

    private bool _isNavigationViewVisible = true;

    public bool IsNavigationViewVisible
    {
        get => _isNavigationViewVisible;
        set => Set(ref _isNavigationViewVisible, value);
    }

    private int _importProgress;

    /// <summary>
    ///     Gets or sets the progress of the current import operation.
    /// </summary>
    public int ImportProgress
    {
        get => _importProgress;
        set => Set(ref _importProgress, value);
    }

    private string _importText;

    /// <summary>
    ///     Gets or sets the text to display while importing audiobooks.
    /// </summary>
    public string ImportText
    {
        get => _importText;
        set => Set(ref _importText, value);
    }

    private string _notificationText;

    public string NotificationText
    {
        get => _notificationText;
        set => Set(ref _notificationText, value);
    }

    // todo: don't need this anymore
    private bool _isNotificationVisible;

    public bool IsNotificationVisible
    {
        get => _isNotificationVisible;
        set => Set(ref _isNotificationVisible, value);
    }

    private InfoBarSeverity _notificationSeverity;

    public InfoBarSeverity NotificationSeverity
    {
        get => _notificationSeverity;
        set => Set(ref _notificationSeverity, value);
    }

    /// <summary>
    ///     Gets the complete list of audiobooks from the database.
    /// </summary>
    public async Task GetAudiobookListAsync()
    {
        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        // NOTE: THIS IS FOR TESTING -> NEED TO REMOVE THIS
#if DEBUG
        // await Task.Delay(TimeSpan.FromSeconds(5));
#endif

        var audiobooks = (await App.Repository.Audiobooks.GetAsync()).AsList();

        await dispatcherQueue.EnqueueAsync(() =>
        {
            ShowStartPanel = audiobooks.Count == 0;

            Audiobooks.Clear();
            foreach (var c in audiobooks) Audiobooks.Add(new AudiobookViewModel(c));
            IsLoading = false;
        });
    }

    /// <summary>
    ///     Saves any modified audiobooks and reloads the audiobook list from the database.
    /// </summary>
    public void Refresh()
    {
        Task.Run(async () =>
        {
            foreach (var modifiedAudiobook in Audiobooks
                         .Where(audiobook => audiobook.IsModified).Select(audiobook => audiobook.Model))
                await App.Repository.Audiobooks.UpsertAsync(modifiedAudiobook);

            await GetAudiobookListAsync();
        });
    }

    /// <summary>
    ///     Deletes the selected audiobook from the database.
    /// </summary>
    public async Task DeleteAudiobookAsync()
    {
        if (SelectedAudiobook == null) return;

        if (SelectedAudiobook == App.PlayerViewModel.NowPlaying)
            dispatcherQueue.TryEnqueue(() =>
            {
                App.PlayerViewModel.MediaPlayer.Pause();
                App.PlayerViewModel.NowPlaying.IsNowPlaying = false;
                App.PlayerViewModel.NowPlaying = null;
            });

        await App.Repository.Audiobooks.DeleteAsync(SelectedAudiobook.Id);
        await App.ViewModel.AppDataService.DeleteCoverImageAsync(SelectedAudiobook.CoverImagePath);

        await GetAudiobookListAsync();

        await dispatcherQueue.EnqueueAsync(() =>
        {
            SelectedAudiobook = null;
            EnqueueNotification(new Notification
            {
                Message = "Audiobook deleted successfully!",
                Severity = InfoBarSeverity.Success
            });
        });
    }

    // todo: fix the bug here and add a confirmation dialog
    public async void DeleteAudiobooksAsync()
    {
        await dispatcherQueue.EnqueueAsync(() =>
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

    public async void ImportAudiobookAsync()
    {
        var importFailed = false;
        var openPicker = new FileOpenPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add(".m4b");

        var file = await openPicker.PickSingleFileAsync();
        if (file == null) return;

        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        MessageService.CancelDialogRequested += () => _cancellationTokenSource.Cancel();

        MessageService.ShowDialog(DialogType.Import, "Importing Audiobooks",
            "Please wait while the audiobooks are imported...");

        try
        {
            await FileImporter.ImportFileAsync(file.Path, token,
                async (progress, total, title, didFail) =>
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        ImportProgress = (int)((double)progress / total * 100);
                        ImportText = $" {title}";
                    });

                    if (didFail)
                    {
                        importFailed = true;
                        EnqueueNotification(new Notification
                        {
                            Message = "Failed to import audiobook. Path: " + file.Path,
                            Severity = InfoBarSeverity.Error
                        });
                    }
                });
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

        await dispatcherQueue.EnqueueAsync(() =>
        {
            ImportText = string.Empty;
            ImportProgress = 0;
            return Task.CompletedTask;
        });

        if (!importFailed)
            EnqueueNotification(new Notification
            {
                Message = "Audiobook imported successfully!",
                Severity = InfoBarSeverity.Success
            });

        await GetAudiobookListAsync();
        
        // select the imported audiobook
        var audiobook = Audiobooks.FirstOrDefault(a => a.FilePath == file.Path);
        if (audiobook != null)
            App.PlayerViewModel.OpenAudiobook(audiobook);
    }

    public async Task<bool> ImportAudiobookTest(string path, bool showImportDialog = true)
    {
        var importFailed = false;

        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        if (showImportDialog)
        {
            MessageService.CancelDialogRequested += () => _cancellationTokenSource.Cancel();

            MessageService.ShowDialog(DialogType.Import, "Importing Audiobooks",
                "Please wait while the audiobooks are imported...");
        }

        try
        {
            await FileImporter.ImportFileAsync(path, token,
                async (progress, total, title, didFail) =>
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        ImportProgress = (int)((double)progress / total * 100);
                        ImportText = $" {title}";
                    });

                    if (didFail)
                    {
                        importFailed = true;
                        EnqueueNotification(new Notification
                        {
                            Message = "Failed to import audiobook. Path: " + path,
                            Severity = InfoBarSeverity.Error
                        });
                    }
                });
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
                Message = "Failed to import audiobook. Path: " + path,
                Severity = InfoBarSeverity.Error
            });
            LoggingService.Log(e.Message);
        }

        await dispatcherQueue.EnqueueAsync(() =>
        {
            ImportText = string.Empty;
            ImportProgress = 0;
            return Task.CompletedTask;
        });

        if (!importFailed)
            EnqueueNotification(new Notification
            {
                Message = "Audiobook imported successfully!",
                Severity = InfoBarSeverity.Success
            });

        await GetAudiobookListAsync();

        return !importFailed;
    }

    private CancellationTokenSource _cancellationTokenSource;

    public async void ImportAudiobooksAsync()
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
        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        MessageService.CancelDialogRequested += () => _cancellationTokenSource.Cancel();

        MessageService.ShowDialog(DialogType.Import, "Importing Audiobooks",
            "Please wait while the audiobooks are imported...");

        var totalBooks = 0;
        var failedBooks = 0;

        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            await FileImporter.ImportDirectoryAsync(folder.Path, token,
                async (progress, total, title, didFail) =>
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        totalBooks++;
                        ImportProgress = ((double)progress / total * 100).ToInt();
                        ImportText = $" {title}";
                    });

                    if (didFail)
                    {
                        totalBooks--;
                        failedBooks++;
                        EnqueueNotification(new Notification
                            { Message = $"Failed to import {title}!", Severity = InfoBarSeverity.Error });
                    }
                });
        }
        catch (OperationCanceledException)
        {
            EnqueueNotification(new Notification
            {
                Message = "Import operation was cancelled!", Severity = InfoBarSeverity.Warning
            });
        }

        await dispatcherQueue.EnqueueAsync(() =>
        {
            ImportText = string.Empty;
            ImportProgress = 0;
        });

        if (failedBooks > 0)
            EnqueueNotification(new Notification
            {
                Message = $"{failedBooks} Audiobooks failed to import!", Severity = InfoBarSeverity.Error
            });

        EnqueueNotification(new Notification
        {
            Message = $"{totalBooks} Audiobooks imported successfully!", Severity = InfoBarSeverity.Success
        });

        await GetAudiobookListAsync();
        
        stopwatch.Stop();
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    /// <summary>
    ///     Resets the audiobook list.
    /// </summary>
    public async Task ResetAudiobookList()
    {
        await dispatcherQueue.EnqueueAsync(async () =>
            await GetAudiobookListAsync());
    }

    // TODO: need to move these methods to a separate class

    // TODO: add type and duration to the Notification class
    // ViewModel.EnqueueNotification(new Notification
    // {
    //     Message = "This is a test notification",
    //     Type = NotificationType.Info,
    //     Duration = TimeSpan.FromSeconds(5)
    // });

    public ObservableCollection<Notification> Notifications { get; } = [];

    public void EnqueueNotification(Notification notification)
    {
        dispatcherQueue.EnqueueAsync(() => { Notifications.Add(notification); });
    }

    // Call this method when the InfoBar is closed
    public void OnNotificationClosed(Notification notification)
    {
        dispatcherQueue.EnqueueAsync(() =>
        {
            Notifications.Remove(notification);
            if (Notifications.Count == 0) Notifications.Clear();
        });
    }
}

public class Notification
{
    public string Message { get; set; }
    public InfoBarSeverity Severity { get; set; }
}