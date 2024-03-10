// Author: rstewa
// Created: 3/5/2024
// Updated: 3/10/2024

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Audibly.App.Services;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
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

    private AudiobookViewModel _selectedAudiobook;

    /// <summary>
    ///     Gets or sets the selected audiobook, or null if no audiobook is selected.
    /// </summary>
    public AudiobookViewModel SelectedAudiobook
    {
        get => _selectedAudiobook;
        set => Set(ref _selectedAudiobook, value);
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

    /// <summary>
    ///     Gets the complete list of audiobooks from the database.
    /// </summary>
    public async Task GetAudiobookListAsync()
    {
        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        var audiobooks = await App.Repository.Audiobooks.GetAsync();

        // todo: fix this bug
        if (audiobooks == null) return;

        await dispatcherQueue.EnqueueAsync(() =>
        {
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

    public async void ImportAudiobooks()
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

        // await dispatcherQueue.EnqueueAsync(() => IsImporting = true);
        await dispatcherQueue.EnqueueAsync(() => IsImporting = true);
        
        await Task.Run(async () =>
        {
            await _fileImporter.ImportAsync(folder.Path, async (progress, total, text) =>
            {
                await dispatcherQueue.EnqueueAsync(() =>
                {
                    ImportProgress = (int)((double)progress / total * 100);
                    IsImportingText = $"Importing {text}...";
                });
            });

            await dispatcherQueue.EnqueueAsync(() => IsImporting = false);

            await GetAudiobookListAsync();
        });
    }
}