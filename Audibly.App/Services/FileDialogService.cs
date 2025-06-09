// Author: rstewa · https://github.com/rstewa
// Updated: 06/09/2025

using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Audibly.App.Services.Interfaces;
using WinRT.Interop;
using StorageFile = Windows.Storage.StorageFile;

namespace Audibly.App.Services;

public class FileDialogService : IFileDialogService
{
    #region IFileDialogService Members

    public StorageFile OpenFileDialog(List<string> fileTypes,
        PickerLocationId locationId = PickerLocationId.Desktop)
    {
        var openPicker = new FileOpenPicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(openPicker, hWnd);
        openPicker.SuggestedStartLocation = locationId;
        openPicker.ViewMode = PickerViewMode.Thumbnail;

        foreach (var fileType in fileTypes) openPicker.FileTypeFilter.Add(fileType);

        return openPicker.PickSingleFileAsync().AsTask().GetAwaiter().GetResult();
    }

    public StorageFile SaveFileDialog(string defaultFileName, List<string> fileTypes,
        PickerLocationId locationId = PickerLocationId.Desktop)
    {
        var savePicker = new FileSavePicker();
        var window = App.Window;
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(savePicker, hWnd);
        savePicker.SuggestedStartLocation = locationId;
        savePicker.SuggestedFileName = defaultFileName;

        foreach (var fileType in fileTypes) savePicker.FileTypeChoices.Add(fileType, new List<string> { fileType });

        return savePicker.PickSaveFileAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion
}