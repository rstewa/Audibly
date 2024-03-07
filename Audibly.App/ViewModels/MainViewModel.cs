// Author: rstewa
// Created: 3/5/2024
// Updated: 3/6/2024

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Audibly.App.Services;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

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

    /// <summary>
    ///     Gets the complete list of audiobooks from the database.
    /// </summary>
    public async Task GetAudiobookListAsync()
    {
        await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

        var audiobooks = await App.Repository.Audiobooks.GetAsync();
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
    public void Sync()
    {
        Task.Run(async () =>
        {
            foreach (var modifiedAudiobook in Audiobooks
                         .Where(audiobook => audiobook.IsModified).Select(audiobook => audiobook.Model))
                await App.Repository.Audiobooks.UpsertAsync(modifiedAudiobook);

            await GetAudiobookListAsync();
        });
    }

    public void ImportAudiobooks()
    {
        Task.Run(async () =>
        {
            // todo: un-hardcode this path
            var path = @"C:\Users\rstewa\Libation\Books";

            await _fileImporter.ImportAsync(path);
        });
    }
}