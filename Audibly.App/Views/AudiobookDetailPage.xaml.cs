// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/16/2024

using System;
using System.Linq;
using Audibly.App.UserControls;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AudiobookDetailPage : Page
{
    /// <summary>
    ///     Initializes the page.
    /// </summary>
    public AudiobookDetailPage()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Used to bind the UI to the data.
    /// </summary>
    public AudiobookViewModel ViewModel { get; set; }

    public MainViewModel MainViewModel => App.ViewModel;

    /// <summary>
    ///     Navigate to the previous page when the user cancels the creation of a new audiobook record.
    /// </summary>
    private void AddNewAudiobookCanceled(object sender, EventArgs e)
    {
        Frame.GoBack();
    }

    /// <summary>
    ///     Displays the selected audiobook data.
    /// </summary>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter == null)
        {
            ViewModel = new AudiobookViewModel
            {
                IsNewAudiobook = true,
                IsInEdit = true
            };
            VisualStateManager.GoToState(this, "NewAudiobook", false);
        }
        else
        {
            ViewModel = App.ViewModel.Audiobooks.Where(
                audiobook => audiobook.Model.Id == (Guid)e.Parameter).First();
        }

        ViewModel.AddNewAudiobookCanceled += AddNewAudiobookCanceled;
        base.OnNavigatedTo(e);
    }

    /// <summary>
    ///     Check whether there are unsaved changes and warn the user.
    /// </summary>
    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        if (ViewModel.IsModified)
        {
            // Cancel the navigation immediately, otherwise it will continue at the await call. 
            e.Cancel = true;

            void resumeNavigation()
            {
                if (e.NavigationMode == NavigationMode.Back)
                    Frame.GoBack();
                else
                    Frame.Navigate(e.SourcePageType, e.Parameter, e.NavigationTransitionInfo);
            }

            var saveDialog = new SaveChangesDialog { Title = "Save changes?" };
            saveDialog.XamlRoot = Content.XamlRoot;
            await saveDialog.ShowAsync();
            var result = saveDialog.Result;

            switch (result)
            {
                case SaveChangesDialogResult.Save:
                    await ViewModel.SaveAsync();
                    resumeNavigation();
                    break;
                case SaveChangesDialogResult.DontSave:
                    await ViewModel.RevertChangesAsync();
                    resumeNavigation();
                    break;
                case SaveChangesDialogResult.Cancel:
                    break;
            }
        }

        base.OnNavigatingFrom(e);
    }

    /// <summary>
    ///     Disconnects the AddNewAudiobookCanceled event handler from the ViewModel
    ///     when the parent frame navigates to a different page.
    /// </summary>
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewModel.AddNewAudiobookCanceled -= AddNewAudiobookCanceled;
        base.OnNavigatedFrom(e);
    }

    /// <summary>
    ///     Initializes the AutoSuggestBox portion of the search box.
    /// </summary>
    private void AudiobookSearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is CollapsibleSearchBox searchBox)
        {
            searchBox.AutoSuggestBox.QuerySubmitted += AudiobookSearchBox_QuerySubmitted;
            searchBox.AutoSuggestBox.TextChanged += AudiobookSearchBox_TextChanged;
            searchBox.AutoSuggestBox.PlaceholderText = "Search audiobooks...";
        }
    }

    /// <summary>
    ///     Queries the database for a audiobook result matching the search text entered.
    /// </summary>
    private async void AudiobookSearchBox_TextChanged(AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        // We only want to get results when it was a user typing,
        // otherwise we assume the value got filled in by TextMemberPath
        // or the handler for SuggestionChosen
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            // If no search query is entered, refresh the complete list.
            if (string.IsNullOrEmpty(sender.Text))
                sender.ItemsSource = null;
            else
                sender.ItemsSource = await App.Repository.Audiobooks.GetAsync(sender.Text);
        }
    }

    /// <summary>
    ///     Search by audiobook name, email, or phone number, then display results.
    /// </summary>
    private void AudiobookSearchBox_QuerySubmitted(AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is Audiobook audiobook) Frame.Navigate(typeof(AudiobookDetailPage), audiobook.Id);
    }
}