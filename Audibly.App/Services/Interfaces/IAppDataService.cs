// Author: rstewa · https://github.com/rstewa
// Updated: 06/09/2025

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATL;
using Audibly.Models;

namespace Audibly.App.Services.Interfaces;

public interface IAppDataService
{
    Task<Tuple<string, string>> WriteCoverImageAsync(string path, byte[]? imageBytes);

    Task DeleteCoverImageAsync(string path);

    Task DeleteCoverImagesAsync(List<string> paths, Func<int, int, string, Task> progressCallback);

    Task WriteMetadataAsync(string path, Track track);

    Task ExportMetadataAsync(List<SourceFile> sourceFiles);
}