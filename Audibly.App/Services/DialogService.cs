// Author: rstewa · https://github.com/rstewa
// Created: 08/21/2024
// Updated: 08/22/2024

using System;
using System.Threading.Tasks;
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
}