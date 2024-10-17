// Author: rstewa · https://github.com/rstewa
// Created: 09/29/2024
// Updated: 10/17/2024

using System;
using System.Threading.Tasks;
using Audibly.App.ViewModels;
using Audibly.App.Views.ControlPages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Services;

public static class DialogService
{
    public static async Task ShowSelectFilesDialogAsync(this FrameworkElement element)
    {
        var selectFilesDialog = new SelectFilesDialog();

        var contentDialog = new ContentDialog
        {
            Title = "Put Selected Files in Order (Drag and Drop)",
            Content = selectFilesDialog,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = element.XamlRoot,
            MinWidth = selectFilesDialog.ActualWidth
        };

        await contentDialog.ShowAsync();
    }

    public static async Task ShowMoreInfoDialogAsync(this FrameworkElement element,
        AudiobookViewModel audiobookViewModel)
    {
        var moreInfoDialog = new MoreInfoDialogContent(audiobookViewModel);

        var contentDialog = new ContentDialog
        {
            Title = "More Info",
            Content = moreInfoDialog,
            CloseButtonText = "Close",
            XamlRoot = element.XamlRoot,
            MinWidth = moreInfoDialog.ActualWidth
        };

        await contentDialog.ShowAsync();
    }
}