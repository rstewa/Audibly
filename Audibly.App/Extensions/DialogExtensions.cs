// Author: rstewa · https://github.com/rstewa
// Created: 01/01/2025
// Updated: 01/01/2025

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Audibly.App.Services;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Extensions;

// from: https://platform.uno/docs/articles/guides/silverlight-migration/07-dialogs.html

public static class DialogExtensions
{
    internal static async Task<ContentDialogResult> ShowOneAtATimeAsync(
        this ContentDialog dialog,
        TimeSpan? timeout = null,
        CancellationToken? token = null)
    {
        try
        {
            return await DialogManager.OneAtATimeAsync(async () => await dialog.ShowAsync(), timeout, token);
        }
        catch (Exception e)
        {
            // log the exception
            App.ViewModel.LoggingService.LogError(e, true);
            return ContentDialogResult.None;
        }
    }

    internal static ContentDialog SetPrimaryButton(this ContentDialog dialog, string text)
    {
        dialog.PrimaryButtonText = text;
        dialog.IsPrimaryButtonEnabled = true;
        return dialog;
    }

    internal static ContentDialog SetCloseButton(this ContentDialog dialog, string text)
    {
        dialog.CloseButtonText = text;
        return dialog;
    }

    internal static ContentDialog SetPrimaryButton(this ContentDialog dialog, string text,
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickHandler)
    {
        dialog.SetPrimaryButton(text);
        dialog.PrimaryButtonClick += clickHandler;
        return dialog;
    }

    internal static ContentDialog SetSecondaryButton(this ContentDialog dialog, string text)
    {
        dialog.SecondaryButtonText = text;
        dialog.IsSecondaryButtonEnabled = true;
        return dialog;
    }

    internal static ContentDialog SetSecondaryButton(this ContentDialog dialog, string text,
        TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickHandler)
    {
        dialog.SetSecondaryButton(text);
        dialog.SecondaryButtonClick += clickHandler;
        return dialog;
    }
}