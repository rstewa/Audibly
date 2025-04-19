// Author: rstewa · https://github.com/rstewa
// Updated: 03/18/2025

using System.Linq;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CollectionContentPage : Page
{
    public CollectionContentPage()
    {
        InitializeComponent();
    }
    
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is CollectionViewModel collectionViewModel)
        {
            FolderNameTextBlock.Text = $"Contents of {collectionViewModel.Name}";
            // TODO: Load and display the contents of the folder
            var collections = await App.Repository.Collections.GetAllChildrenAsync(collectionViewModel.Id);
            var collectionViewModels = collections.Select(f => new CollectionViewModel(f));
            
        }
    }
}