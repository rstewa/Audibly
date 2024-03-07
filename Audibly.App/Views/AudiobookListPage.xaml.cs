//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//  The MIT License (MIT)
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace Audibly.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AudiobookListPage : Page
    {
        private DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        /// <summary>
        /// Initializes the page.
        /// </summary>
        public AudiobookListPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the app-wide ViewModel instance.
        /// </summary>
        public MainViewModel ViewModel => App.ViewModel;

        /// <summary>
        /// Initializes the AutoSuggestBox portion of the search box.
        /// </summary>
        private void AudiobookSearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (AudiobookSearchBox != null)
            {
                AudiobookSearchBox.AutoSuggestBox.QuerySubmitted += AudiobookSearchBox_QuerySubmitted;
                AudiobookSearchBox.AutoSuggestBox.TextChanged += AudiobookSearchBox_TextChanged;
                AudiobookSearchBox.AutoSuggestBox.PlaceholderText = "Search audiobooks...";
            }
        }

        /// <summary>
        /// Updates the search box items source when the user changes the search text.
        /// </summary>
        private async void AudiobookSearchBox_TextChanged(AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args)
        {
            // We only want to get results when it was a user typing,
            // otherwise we assume the value got filled in by TextMemberPath
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // If no search query is entered, refresh the complete list.
                if (String.IsNullOrEmpty(sender.Text))
                {
                    await dispatcherQueue.EnqueueAsync(async () =>
                        await ViewModel.GetAudiobookListAsync());
                    sender.ItemsSource = null;
                }
                else
                {
                    string[] parameters = sender.Text.Split(new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    sender.ItemsSource = ViewModel.Audiobooks
                        .Where(audiobook => parameters.Any(parameter =>
                            audiobook.Author.StartsWith(parameter) ||
                            audiobook.Title.StartsWith(parameter)))
                        .OrderByDescending(audiobook => parameters.Count(parameter =>
                            audiobook.Author.StartsWith(parameter) ||
                            audiobook.Title.StartsWith(parameter)))
                        .Select(audiobook => $"{audiobook.Title} : {audiobook.Author}"); 
                }
            }
        }

        /// Filters or resets the audiobook list based on the search text.
        /// </summary>
        private async void AudiobookSearchBox_QuerySubmitted(AutoSuggestBox sender,
            AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (String.IsNullOrEmpty(args.QueryText))
            {
                await ResetAudiobookList();
            }
            else
            {
                await FilterAudiobookList(args.QueryText);
            }
        }

        /// <summary>
        /// Resets the audiobook list.
        /// </summary>
        private async Task ResetAudiobookList()
        {
            await dispatcherQueue.EnqueueAsync(async () =>
                await ViewModel.GetAudiobookListAsync());
        }

        /// <summary>
        /// Filters the audiobook list based on the search text.
        /// </summary>
        private async Task FilterAudiobookList(string text)
        {
            string[] parameters = text.Split(new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);

            var matches = ViewModel.Audiobooks.Where(audiobook => parameters
                .Any(parameter =>
                            audiobook.Author.StartsWith(parameter) ||
                            audiobook.Title.StartsWith(parameter)))
                .OrderByDescending(audiobook => parameters.Count(parameter =>
                            audiobook.Author.StartsWith(parameter) ||
                            audiobook.Title.StartsWith(parameter)))
                .ToList();

            await dispatcherQueue.EnqueueAsync(() =>
            {
                ViewModel.Audiobooks.Clear();
                foreach (var match in matches)
                {
                    ViewModel.Audiobooks.Add(match);
                }
            });
        }

        /// <summary>
        /// Resets the audiobook list when leaving the page.
        /// </summary>
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await ResetAudiobookList();
        }

        /// <summary>
        /// Applies any existing filter when navigating to the page.
        /// </summary>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(AudiobookSearchBox.AutoSuggestBox.Text))
            {
                await FilterAudiobookList(AudiobookSearchBox.AutoSuggestBox.Text);
            }
        }

        /// <summary>
        /// Menu flyout click control for selecting a audiobook and displaying details.
        /// </summary>
        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAudiobook != null)
            {
                Frame.Navigate(typeof(AudiobookDetailPage), ViewModel.SelectedAudiobook.Model.Id,
                    new DrillInNavigationTransitionInfo());
            }
        }

        private void DataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) =>
            Frame.Navigate(typeof(AudiobookDetailPage), ViewModel.SelectedAudiobook.Model.Id,
                    new DrillInNavigationTransitionInfo());

        /// <summary>
        /// Navigates to a blank audiobook details page for the user to fill in.
        /// </summary>
        private void CreateAudiobook_Click(object sender, RoutedEventArgs e) =>
            Frame.Navigate(typeof(AudiobookDetailPage), null, new DrillInNavigationTransitionInfo());

        /// <summary>
        /// Reverts all changes to the row if the row has changes but a cell is not currently in edit mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape &&
                ViewModel.SelectedAudiobook != null &&
                ViewModel.SelectedAudiobook.IsModified &&
                !ViewModel.SelectedAudiobook.IsInEdit)
            {
                (sender as DataGrid).CancelEdit(DataGridEditingUnit.Row);
            }
        }

        /// <summary>
        /// Selects the tapped audiobook. 
        /// </summary>
        private void DataGrid_RightTapped(object sender, RightTappedRoutedEventArgs e) =>
            ViewModel.SelectedAudiobook = (e.OriginalSource as FrameworkElement).DataContext as AudiobookViewModel;

        /// <summary>
        /// Sorts the data in the DataGrid.
        /// </summary>
        private void DataGrid_Sorting(object sender, DataGridColumnEventArgs e) =>
            (sender as DataGrid).Sort(e.Column, ViewModel.Audiobooks.Sort);
    }
}
