// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/24/2024

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Audibly.App.Services;
using Audibly.App.Views.ControlPages;
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
    private readonly IImportFiles _fileImporter;

    /// <summary>
    ///     Creates a new MainViewModel.
    /// </summary>
    public MainViewModel(IImportFiles fileImporter)
    {
        _fileImporter = fileImporter;
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

    private int _importProgress;

    /// <summary>
    ///     Gets or sets the progress of the current import operation.
    /// </summary>
    public int ImportProgress
    {
        get => _importProgress;
        set => Set(ref _importProgress, value);
    }

    private string _isImportingText;

    /// <summary>
    ///     Gets or sets the text to display while importing audiobooks.
    /// </summary>
    public string IsImportingText
    {
        get => _isImportingText;
        set => Set(ref _isImportingText, value);
    }
    
    private string _notificationText;
    
    public string NotificationText
    {
        get => _notificationText;
        set => Set(ref _notificationText, value);
    }
    
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

        // todo: fix this bug
        // if (audiobooks == null) return;

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

    public async void DeleteAudiobooksAsync()
    {
        await dispatcherQueue.EnqueueAsync(() => SelectedAudiobook = null);

        await Task.Run(async () =>
        {
            var count = 0;
            foreach (var audiobook in Audiobooks)
            {
                await App.Repository.Audiobooks.DeleteAsync(audiobook.Id);
                count++;
            }
            
            await GetAudiobookListAsync();
            
            await dispatcherQueue.EnqueueAsync(() =>
            {
                NotificationText = $"{count} Audiobooks deleted successfully!";
                NotificationSeverity = InfoBarSeverity.Success;
                IsNotificationVisible = true;
            });
        });
    }
    
    public async void ImportAudiobookAsync()
    {
        // Create a folder picker
        var openPicker = new FileOpenPicker();

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.Window;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WindowNative.GetWindowHandle(window);

        // Initialize the folder picker with the window handle (HWND).
        InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        // todo: maybe remove this; trying it out
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add(".m4b");

        // Open the picker for the user to pick a folder
        var file = await openPicker.PickSingleFileAsync();

        if (file == null) return;
        
        // todo: switch the result

        await dispatcherQueue.EnqueueAsync(() => IsImporting = true);

        await Task.Run(async () =>
        {
            await _fileImporter.ImportFileAsync(file.Path, async (progress, total, text) =>
            {
                await dispatcherQueue.EnqueueAsync(() =>
                {
                    ImportProgress = (int)((double)progress / total * 100);
                    IsImportingText = $"Importing {text}...";
                });
            });

            await dispatcherQueue.EnqueueAsync(() => IsImporting = false);

            await GetAudiobookListAsync();
            
            await dispatcherQueue.EnqueueAsync(() =>
            {
                NotificationText = "Audiobook imported successfully!";
                NotificationSeverity = InfoBarSeverity.Success;
                IsNotificationVisible = true;
            });
        });
    }

    public async void ImportAudiobooksAsync()
    {
        // Create a folder picker
        var openPicker = new FolderPicker();

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.Window;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WindowNative.GetWindowHandle(window);

        // Initialize the folder picker with the window handle (HWND).
        InitializeWithWindow.Initialize(openPicker, hWnd);

        // Set options for your folder picker
        openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        // todo: maybe remove this; trying it out
        openPicker.ViewMode = PickerViewMode.Thumbnail;
        openPicker.FileTypeFilter.Add("*");

        // Open the picker for the user to pick a folder
        var folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
        else
            return;
        
        // todo: switch the result

        await dispatcherQueue.EnqueueAsync(() => IsImporting = true);

        var totalBooks = 0;
        await Task.Run(async () =>
        {
            await _fileImporter.ImportDirectoryAsync(folder.Path, async (progress, total, text) =>
            {
                await dispatcherQueue.EnqueueAsync(() =>
                {
                    totalBooks = total;
                    ImportProgress = (int)((double)progress / total * 100);
                    IsImportingText = $"Importing {text}...";
                });
            });

            await dispatcherQueue.EnqueueAsync(() => IsImporting = false);

            await GetAudiobookListAsync();
            
            await dispatcherQueue.EnqueueAsync(() =>
            {
                NotificationText = $"{totalBooks} Audiobooks imported successfully!";
                NotificationSeverity = InfoBarSeverity.Success;
                IsNotificationVisible = true;
            });
        });
    }
}