// Author: rstewa · https://github.com/rstewa
// Created: 3/27/2024
// Updated: 3/28/2024

using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Library : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    public Library()
    {
        InitializeComponent();
    }

    private void AudiobookSearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        ;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}