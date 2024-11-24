// Author: rstewa · https://github.com/rstewa
// Created: 3/25/2024
// Updated: 3/25/2024

using Audibly.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views.ControlPages;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ProgressDialogContent : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    public ProgressDialogContent()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }
}