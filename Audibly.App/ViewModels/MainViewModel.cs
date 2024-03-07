using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

namespace Audibly.App.ViewModels
{
    /// <summary>
    /// Provides data and commands accessible to the entire app.  
    /// </summary>
    public class MainViewModel : BindableBase
    {
        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// Creates a new MainViewModel.
        /// </summary>
        public MainViewModel() => Task.Run(GetAudiobookListAsync);

        /// <summary>
        /// The collection of audiobooks in the list. 
        /// </summary>
        public ObservableCollection<AudiobookViewModel> Audiobooks { get; }
            = new ObservableCollection<AudiobookViewModel>();

        private AudiobookViewModel _selectedAudiobook;

        /// <summary>
        /// Gets or sets the selected audiobook, or null if no audiobook is selected. 
        /// </summary>
        public AudiobookViewModel SelectedAudiobook
        {
            get => _selectedAudiobook;
            set => Set(ref _selectedAudiobook, value);
        }

        private bool _isLoading = false;

        /// <summary>
        /// Gets or sets a value indicating whether the Audiobooks list is currently being updated. 
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading; 
            set => Set(ref _isLoading, value);
        }

        /// <summary>
        /// Gets the complete list of audiobooks from the database.
        /// </summary>
        public async Task GetAudiobookListAsync()
        {
            await dispatcherQueue.EnqueueAsync(() => IsLoading = true);

            var audiobooks = await App.Repository.Audiobooks.GetAsync();
            if (audiobooks == null)
            {
                return;
            }

            await dispatcherQueue.EnqueueAsync(() =>
            {
                Audiobooks.Clear();
                foreach (var c in audiobooks)
                {
                    Audiobooks.Add(new AudiobookViewModel(c));
                }
                IsLoading = false;
            });
        }

        /// <summary>
        /// Saves any modified audiobooks and reloads the audiobook list from the database.
        /// </summary>
        public void Sync()
        {
            Task.Run(async () =>
            {
                foreach (var modifiedAudiobook in Audiobooks
                    .Where(audiobook => audiobook.IsModified).Select(audiobook => audiobook.Model))
                {
                    await App.Repository.Audiobooks.UpsertAsync(modifiedAudiobook);
                }

                await GetAudiobookListAsync();
            });
        }
    }
}
