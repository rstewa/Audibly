// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Audibly.App.Services.Interfaces;

public interface IImportFiles
{
    Task ImportDirectoryAsync(string path, Func<int, int, string, List<string>, Task> progressCallback);

    Task<bool> ImportFileAsync(string path, Func<int, int, string, Task> progressCallback);
}