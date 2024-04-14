// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/13/2024

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Audibly.App.Services.Interfaces;

public interface IImportFiles
{
    public delegate void ImportCompletedHandler();

    public event ImportCompletedHandler ImportCompleted;

    Task ImportDirectoryAsync(string path, CancellationToken cancellationToken,
        Func<int, int, string, bool, Task> progressCallback);

    Task ImportFileAsync(string path, CancellationToken cancellationToken,
        Func<int, int, string, bool, Task> progressCallback);
}