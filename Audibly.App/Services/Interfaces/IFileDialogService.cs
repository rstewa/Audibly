// Author: rstewa · https://github.com/rstewa
// Updated: 06/09/2025

using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Audibly.App.Services.Interfaces;

public interface IFileDialogService
{
    public StorageFile OpenFileDialog(List<string> fileTypes,
        PickerLocationId locationId = PickerLocationId.Desktop);

    public StorageFile SaveFileDialog(string defaultFileName, List<string> fileTypes,
        PickerLocationId locationId = PickerLocationId.Desktop);
}