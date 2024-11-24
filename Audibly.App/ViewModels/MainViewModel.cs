// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/14/2024

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    public delegate void ProgressDialogCompletedHandler();

    public event ProgressDialogCompletedHandler? ProgressDialogCompleted;

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
        // Task.Run(GetAudiobookListAsync(firstRun: true));
        Task.Run(() => GetAudiobookListAsync(true));
    }

    /// <summary>
    ///     The collection of audiobooks in the list.
    /// </summary>
    public ObservableCollection<AudiobookViewModel> Audiobooks { get; } = [];

    public bool NeedToImportAudiblyExport { get; set; } = false;

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

    private int _progressDialogProgress;

    /// <summary>
    ///     Gets or sets the progress of the current import operation.
    /// </summary>
    public int ProgressDialogProgress
    {
        get => _progressDialogProgress;
        set => Set(ref _progressDialogProgress, value);
    }

    private string _progressDialogText;

    /// <summary>
    ///     Gets or sets the text to display while importing audiobooks.
    /// </summary>
    public string ProgressDialogText
    {
        get => _progressDialogText;
        set => Set(ref _progressDialogText, value);
    }

    private string _progressDialogPrefix;

    /// <summary>
    ///     Gets or sets the prefix to display before the progress dialog text.
    /// </summary>
    public string ProgressDialogPrefix
    {
        get => _progressDialogPrefix;
        set => Set(ref _progressDialogPrefix, value);
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
    ///     Invokes the ProgressDialogCompleted event.
    /// </summary>
    public void OnProgressDialogCompleted()
    {
        ProgressDialogCompleted?.Invoke();
    }

    /// <summary>
    ///     Gets the complete list of audiobooks from the database.
    /// </summary>
    public async Task GetAudiobookListAsync(bool firstRun = false)
    {
        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        // if (NeedToImportAudiblyExport)
        // {
        //     var file = ApplicationData.Current.LocalFolder.GetFileAsync("audibly_export.audibly").AsTask().Result;
        //     if (file != null)
        //     {
        //         await ImportFromJsonFileAsync(file);
        //         NeedToImportAudiblyExport = false;
        //     }
        // }

        var audiobooks = (await App.Repository.Audiobooks.GetAsync()).AsList();

        await dispatcherQueue.EnqueueAsync(() =>
        {
            ShowStartPanel = audiobooks.Count == 0;

            Audiobooks.Clear();
            foreach (var c in audiobooks) Audiobooks.Add(new AudiobookViewModel(c));

            if (firstRun)
            {
                var nowPlaying = Audiobooks.FirstOrDefault(x => x.IsNowPlaying);
                if (nowPlaying != null)
                    _ = App.PlayerViewModel.OpenAudiobook(nowPlaying);
            }

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

        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        MessageService.CancelDialogRequested += () => _cancellationTokenSource.Cancel();

        MessageService.ShowDialog(DialogType.Import, "Importing Audiobooks",
            "Please wait while the audiobooks are imported...");

        await ImportFileAsync(file, token);
    }

    public async Task ImportAudiobookFromFileActivationAsync(string path, bool showImportDialog = true)
    {
        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        if (showImportDialog)
        {
            MessageService.CancelDialogRequested += () => _cancellationTokenSource.Cancel();

            MessageService.ShowDialog(DialogType.Import, "Importing Audiobooks",
                "Please wait while the audiobooks are imported...");
        }

        var file = await StorageFile.GetFileFromPathAsync(path);

        await ImportFileAsync(file, token);
    }

    private async Task ImportFileAsync(StorageFile file, CancellationToken token)
    {
        var importFailed = false;
        try
        {
            await FileImporter.ImportFileAsync(file.Path, token,
                async (progress, total, title, didFail) =>
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        ProgressDialogProgress = (int)((double)progress / total * 100);
                        ProgressDialogText = $"Importing {title}";
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
            ProgressDialogText = string.Empty;
            ProgressDialogProgress = 0;
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
        var audiobook = Audiobooks.FirstOrDefault(a => a.CurrentSourceFile.FilePath == file.Path);
        if (audiobook != null)
            await App.PlayerViewModel.OpenAudiobook(audiobook);
    }

    private CancellationTokenSource _cancellationTokenSource;

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
                        ProgressDialogProgress = ((double)progress / total * 100).ToInt();
                        ProgressDialogText = $"Importing {title}";
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
            ProgressDialogText = string.Empty;
            ProgressDialogProgress = 0;
        });

        // if (failedBooks > 0)
        //     EnqueueNotification(new Notification
        //     {
        //         Message = $"{failedBooks} Audiobooks failed to import!", Severity = InfoBarSeverity.Error
        //     });

        EnqueueNotification(new Notification
        {
            Message = $"{totalBooks} Audiobooks imported successfully!", Severity = InfoBarSeverity.Success
        });

        await GetAudiobookListAsync();

        stopwatch.Stop();
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    public ObservableCollection<SelectedFile> SelectedFiles { get; } = [];

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

        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        // get framework element from sender
        var element = sender as FrameworkElement;
        if (element == null) return;

        var result = await element.ShowSelectFilesDialogAsync();
        if (result == ContentDialogResult.None)
        {
            await dispatcherQueue.EnqueueAsync(() => IsLoading = false);
            return;
        }

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
            var filesArray = SelectedFiles.Select(file => file.FilePath).ToArray();
            await FileImporter.ImportFromMultipleFilesAsync(filesArray, token,
                async (progress, total, title, didFail) =>
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        totalBooks++;
                        ProgressDialogProgress = ((double)progress / total * 100).ToInt();
                        ProgressDialogText = $"Importing {title}";
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
        finally
        {
            // clear selected files
            SelectedFiles.Clear();
        }

        await dispatcherQueue.EnqueueAsync(() =>
        {
            ProgressDialogText = string.Empty;
            ProgressDialogProgress = 0;
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
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed} seconds.");
    }

    /// <summary>
    ///     Resets the audiobook list.
    /// </summary>
    public async Task ResetAudiobookListAsync()
    {
        await dispatcherQueue.EnqueueAsync(async () =>
            await GetAudiobookListAsync());
    }

    // TODO: need to move these methods to a separate class

    public ObservableCollection<Notification> Notifications { get; } = [];

    public void EnqueueNotification(Notification notification)
    {
        dispatcherQueue.EnqueueAsync(async () =>
        {
            Notifications.Add(notification);
            await Task.Delay(notification.Duration);
            Notifications.Remove(notification);
        });
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

    public void CreateExportFile(object sender, RoutedEventArgs e)
    {
        _ = CreateExportFileInAppFolder();
    }

    public async Task<string> CreateExportFileInAppFolder(bool letUserChooseLocation = true)
    {
        var audiobookRecords = (await App.Repository.Audiobooks.GetAsync()).AsList();
        Audiobooks.Clear();
        foreach (var c in audiobookRecords) Audiobooks.Add(new AudiobookViewModel(c));

        // get the filepath and currenttimems for each audiobook and write it to a json file
        var audiobooks = Audiobooks.Select(a =>
            new { a.CurrentSourceFile.FilePath, a.CurrentSourceFile.CurrentTimeMs });
        var json = JsonSerializer.Serialize(audiobooks);

        StorageFile file;
        if (!letUserChooseLocation)
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Exports",
                CreationCollisionOption.OpenIfExists);
            file = await folder.CreateFileAsync("audibly_export.audibly", CreationCollisionOption.ReplaceExisting);
        }
        else
        {
            // let user choose where to save the file
            var savePicker = new FileSavePicker();
            var window = App.Window;
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(savePicker, hWnd);
            savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            savePicker.FileTypeChoices.Add("Audibly Export File", new List<string> { ".audibly" });

            file = savePicker.PickSaveFileAsync().AsTask().Result;
        }

        if (file == null) return string.Empty;

        // write the json string to the file
        FileIO.WriteTextAsync(file, json).AsTask().Wait();

        // notify the user that the file was created
        EnqueueNotification(new Notification
        {
            Message = "Export file created successfully!",
            Severity = InfoBarSeverity.Success
        });

        return file.Path;
    }


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

    public async Task ImportFromJsonFileAsync(StorageFile file)
    {
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
            await FileImporter.ImportFromJsonAsync(file, token,
                async (progress, total, title, didFail) =>
                {
                    await dispatcherQueue.EnqueueAsync(() =>
                    {
                        totalBooks++;
                        ProgressDialogProgress = ((double)progress / total * 100).ToInt();
                        ProgressDialogText = $"Importing {title}";
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
            ProgressDialogText = string.Empty;
            ProgressDialogProgress = 0;
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
        LoggingService.Log($"Imported {totalBooks} audiobooks in {stopwatch.Elapsed} seconds.");
    }
}

public class Notification
{
    public string Message { get; set; }
    public InfoBarSeverity Severity { get; set; }
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(10); // Default duration of 10 seconds
}