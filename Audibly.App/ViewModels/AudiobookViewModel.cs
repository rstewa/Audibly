// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/21/2024

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Audibly.App.Extensions;
using Audibly.Models;
using Microsoft.UI.Dispatching;
using ChapterInfo = Audibly.Models.ChapterInfo;

namespace Audibly.App.ViewModels;

/// <summary>
///     Provides a bindable wrapper for the Customer model class, encapsulating various services for access by the UI.
/// </summary>
public class AudiobookViewModel : BindableBase, IEditableObject
{
    private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

    /// <summary>
    ///     Initializes a new instance of the CustomerViewModel class that wraps a Customer object.
    /// </summary>
    public AudiobookViewModel(Audiobook model = null)
    {
        Model = model ?? new Audiobook();

        if (model == null) return;

        Chapters.Clear();
        foreach (var chapter in model.Chapters) Chapters.Add(chapter);
    }

    private Audiobook _model;

    /// <summary>
    ///     Gets or sets the underlying Audiobook object.
    /// </summary>
    public Audiobook Model
    {
        get => _model;
        set
        {
            if (_model != value)
            {
                _model = value;

                // Raise the PropertyChanged event for all properties.
                OnPropertyChanged(string.Empty);
            }
        }
    }

    /// <summary>
    ///     Gets or sets the author of the audiobook.
    /// </summary>
    public string Author
    {
        get => Model.Author;
        set
        {
            if (value != Model.Author)
            {
                Model.Author = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the description of the audiobook.
    /// </summary>
    public string Description
    {
        get => Model.Description;
        set
        {
            if (value != Model.Description)
            {
                Model.Description = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the duration of the audiobook.
    /// </summary>
    public long Duration
    {
        get => Model.Duration;
        set
        {
            if (value != Model.Duration)
            {
                Model.Duration = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Gets the duration of the audiobook as a string in the format "hh:mm:ss".
    /// </summary>
    public string DurationStr => Model.Duration.ToStr_s();

    /// <summary>
    ///     Gets or sets the current time in milliseconds of the audiobook.
    /// </summary>
    public int CurrentTimeMs
    {
        get => Model.CurrentTimeMs;
        set
        {
            if (value != Model.CurrentTimeMs)
            {
                Model.CurrentTimeMs = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    // todo: convert to image to use here
    /// <summary>
    ///     Gets or sets the cover image path of the audiobook.
    /// </summary>
    public string CoverImagePath
    {
        get => Model.CoverImagePath;
        set
        {
            if (value != Model.CoverImagePath)
            {
                Model.CoverImagePath = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current position in the book.
    /// </summary>
    // public string CurrentPositionInBook
    // {
    //     get => Model.CurrentPositionInBook;
    //     set
    //     {
    //         if (value != Model.CurrentPositionInBook)
    //         {
    //             Model.CurrentPositionInBook = value;
    //             IsModified = true;
    //             OnPropertyChanged();
    //         }
    //     }
    // }

    /// <summary>
    ///     Gets or sets the file path of the audiobook.
    /// </summary>
    public string FilePath
    {
        get => Model.FilePath;
        set
        {
            if (value != Model.FilePath)
            {
                Model.FilePath = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the playback speed of the audiobook.
    /// </summary>
    public double PlaybackSpeed
    {
        get => Model.PlaybackSpeed;
        set
        {
            if (value != Model.PlaybackSpeed)
            {
                Model.PlaybackSpeed = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the title of the audiobook.
    /// </summary>
    public string Title
    {
        get => Model.Title ?? string.Empty;
        set
        {
            if (value != Model.Title)
            {
                Model.Title = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }
    
    private string _volumeGlyph;
    public string VolumeGlyph
    {
        get => _volumeGlyph;
        set => Set(ref _volumeGlyph, value);
    }

    /// <summary>
    ///     Gets or sets the volume of the audiobook.
    /// </summary>
    public double Volume
    {
        get => Model.Volume;
        set
        {
            if (value != Model.Volume)
            {
                Model.Volume = value;
                VolumeGlyph = value switch
                {
                    0 => "\uE992",
                    < 0.5 => "\uE993",
                    _ => "\uE994"
                };
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the current chapter of the audiobook.
    /// </summary>
    public ChapterInfo CurrentChapter
    {
        get => Model.Chapters[Model.CurrentChapterIndex ?? 0];
        set
        {
            Model.CurrentChapterIndex = Model.Chapters.IndexOf(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Gets or sets the current chapter index of the audiobook.
    /// </summary>
    public int? CurrentChapterIndex
    {
        get => Model.CurrentChapterIndex;
        set
        {
            Model.CurrentChapterIndex = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ChapterInfo> Chapters { get; set; } = new();

    private ChapterInfo _selectedChapter;

    /// <summary>
    ///     Gets or sets the selected chapter of the audiobook.
    /// </summary>
    public ChapterInfo SelectedChapter
    {
        get => _selectedChapter;
        set => Set(ref _selectedChapter, value);
    }

    /// <summary>
    ///     Gets or sets a value that indicates whether the underlying model has been modified.
    /// </summary>
    /// <remarks>
    ///     Used when sync'ing with the server to reduce load and only upload the models that have changed.
    /// </remarks>
    public bool IsModified { get; set; }

    private bool _isLoading;

    /// <summary>
    ///     Gets or sets a value that indicates whether to show a progress bar.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => Set(ref _isLoading, value);
    }

    private bool _isNewAudiobook;

    /// <summary>
    ///     Gets or sets a value that indicates whether this is a new audiobook.
    /// </summary>
    public bool IsNewAudiobook
    {
        get => _isNewAudiobook;
        set => Set(ref _isNewAudiobook, value);
    }

    private bool _isInEdit;

    /// <summary>
    ///     Gets or sets a value that indicates whether the audiobook data is being edited.
    /// </summary>
    public bool IsInEdit
    {
        get => _isInEdit;
        set => Set(ref _isInEdit, value);
    }

    /// <summary>
    ///     Saves audiobook data that has been edited.
    /// </summary>
    public async Task SaveAsync()
    {
        IsInEdit = false;
        IsModified = false;
        if (IsNewAudiobook)
        {
            IsNewAudiobook = false;
            App.ViewModel.Audiobooks.Add(this);
        }

        await App.Repository.Audiobooks.UpsertAsync(Model);
    }

    /// <summary>
    ///     Raised when the user cancels the changes they've made to the audiobook data.
    /// </summary>
    public event EventHandler AddNewAudiobookCanceled;

    /// <summary>
    ///     Cancels any in progress edits.
    /// </summary>
    public async Task CancelEditsAsync()
    {
        if (IsNewAudiobook)
            AddNewAudiobookCanceled?.Invoke(this, EventArgs.Empty);
        else
            await RevertChangesAsync();
    }

    /// <summary>
    ///     Discards any edits that have been made, restoring the original values.
    /// </summary>
    public async Task RevertChangesAsync()
    {
        IsInEdit = false;
        if (IsModified)
        {
            await RefreshAudiobookAsync();
            IsModified = false;
        }
    }

    /// <summary>
    ///     Enables edit mode.
    /// </summary>
    public void StartEdit()
    {
        IsInEdit = true;
    }

    /// <summary>
    ///     Reloads all of the audiobook data.
    /// </summary>
    public async Task RefreshAudiobookAsync()
    {
        Model = await App.Repository.Audiobooks.GetAsync(Model.Id);
    }

    /// <summary>
    ///     Called when a bound DataGrid control causes the audiobook to enter edit mode.
    /// </summary>
    public void BeginEdit()
    {
        // Not used.
    }

    /// <summary>
    ///     Called when a bound DataGrid control cancels the edits that have been made to a audiobook.
    /// </summary>
    public async void CancelEdit()
    {
        await CancelEditsAsync();
    }

    /// <summary>
    ///     Called when a bound DataGrid control commits the edits that have been made to a audiobook.
    /// </summary>
    public async void EndEdit()
    {
        await SaveAsync();
    }
}