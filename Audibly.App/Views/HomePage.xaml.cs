// Author: rstewa · https://github.com/rstewa
// Created: 07/24/2024
// Updated: 07/24/2024

using System.Collections.Generic;
using System.Linq;
using Audibly.App.ViewModels;
using Audibly.Models;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;
    
    // TODO: this is temporary
    // return the 1st 10 audiobooks in ViewModel.Audiobooks
    public List<AudiobookViewModel> Audiobooks => ViewModel.Audiobooks.Take(10).ToList();

    public HomePage()
    {
        InitializeComponent();
    }

    private void RecentGrid_ItemClick(object sender, ItemClickEventArgs e)
    {
        ;
    }
}