// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;
using System.Threading.Tasks;

namespace Audibly.App.Services;

public interface IImportFiles
{
    Task ImportDirectoryAsync(string path, Func<int, int, string, Task> progressCallback);

    Task ImportFileAsync(string path, Func<int, int, string, Task> progressCallback);
}